import { useState, useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { AxiosError } from 'axios';
import { journeyApi, authApi } from '../../services/api';
import { useAuth } from '../../contexts/AuthContext';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import { extractErrorMessages } from '../../utils/errorHandler';
import toast from 'react-hot-toast';
import './JourneyActions.css';

interface JourneyActionsProps {
  journeyId: string;
  journeyUserId: string;
  isFavorite?: boolean;
  onShareSuccess?: () => void;
}

const JourneyActions = ({
  journeyId,
  journeyUserId,
  isFavorite: initialIsFavorite = false,
  onShareSuccess,
}: JourneyActionsProps) => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const confirmDialog = useConfirmDialog();
  const [showShareModal, setShowShareModal] = useState(false);
  const [showPublicLinkModal, setShowPublicLinkModal] = useState(false);
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [publicLink, setPublicLink] = useState<string | null>(null);
  const [isFavorite, setIsFavorite] = useState(initialIsFavorite);
  const [shareError, setShareError] = useState<string | null>(null);

  const isOwner = user?.userId === journeyUserId;

  useEffect(() => {
    setIsFavorite(initialIsFavorite);
  }, [initialIsFavorite]);

  const {
    data: users,
    isLoading: usersLoading,
    error: usersError,
    refetch: refetchUsers,
  } = useQuery(['users', showShareModal], () => authApi.getUsers().then((res) => res.data), {
    enabled: showShareModal,
    staleTime: 0,
    cacheTime: 5 * 60 * 1000,
    retry: false,
    refetchOnMount: 'always',
  });

  useEffect(() => {
    if (showShareModal) {
      refetchUsers();
    }
  }, [showShareModal, refetchUsers]);

  const favoriteMutation = useMutation(() => journeyApi.addFavorite(journeyId), {
    onSuccess: () => {
      setIsFavorite(true);
      queryClient.invalidateQueries(['journey', journeyId]);
    },
  });

  const unfavoriteMutation = useMutation(() => journeyApi.removeFavorite(journeyId), {
    onSuccess: () => {
      setIsFavorite(false);
      queryClient.invalidateQueries(['journey', journeyId]);
    },
  });

  const shareMutation = useMutation((userIds: string[]) => journeyApi.share(journeyId, userIds), {
    onSuccess: () => {
      setShowShareModal(false);
      setSelectedUserIds([]);
      setSearchTerm('');
      setShareError(null);
      queryClient.invalidateQueries('journeys');
      queryClient.invalidateQueries(['journey', journeyId]);
      onShareSuccess?.();
      toast.success('Journey shared successfully!');
    },
    onError: (error: unknown) => {
      const { message } = extractErrorMessages(error);
      setShareError(message);
      toast.error(message);
    },
  });

  const publicLinkMutation = useMutation(() => journeyApi.generatePublicLink(journeyId), {
    onSuccess: (response) => {
      // Extract token from API URL or use token directly
      const apiUrl = response.data.url;
      const token = response.data.token || apiUrl.split('/').pop() || '';
      const frontendUrl = `${window.location.origin}/journeys/public/${token}`;
      setPublicLink(frontendUrl);
      setShowPublicLinkModal(true);
      queryClient.invalidateQueries(['journey', journeyId]);
    },
  });

  const revokeLinkMutation = useMutation(() => journeyApi.revokePublicLink(journeyId), {
    onSuccess: () => {
      setPublicLink(null);
      setShowPublicLinkModal(false);
      queryClient.invalidateQueries(['journey', journeyId]);
    },
  });

  const handleShare = () => {
    setShareError(null);

    if (selectedUserIds.length === 0) {
      setShareError('Please select at least one user');
      return;
    }

    shareMutation.mutate(selectedUserIds);
  };

  const toggleUserSelection = (userId: string) => {
    setSelectedUserIds((prev) =>
      prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]
    );
  };

  const filteredUsers =
    users?.filter((user) => {
      const searchLower = searchTerm.toLowerCase();
      return (
        user.name.toLowerCase().includes(searchLower) ||
        user.email.toLowerCase().includes(searchLower) ||
        user.username.toLowerCase().includes(searchLower)
      );
    }) || [];

  const handleToggleFavorite = () => {
    if (isFavorite) {
      unfavoriteMutation.mutate();
    } else {
      favoriteMutation.mutate();
    }
  };

  const handleCopyLink = () => {
    if (publicLink) {
      navigator.clipboard.writeText(publicLink);
      toast.success('Link copied to clipboard!');
    }
  };

  return (
    <div className="journey-actions">
      <button
        onClick={handleToggleFavorite}
        className={`btn btn-icon ${isFavorite ? 'btn-favorite-active' : 'btn-secondary'}`}
        title={isFavorite ? 'Remove from favorites' : 'Add to favorites'}
        disabled={favoriteMutation.isLoading || unfavoriteMutation.isLoading}
      >
        {isFavorite ? '⭐' : '☆'}
      </button>

      {isOwner && (
        <>
          <button
            onClick={() => setShowShareModal(true)}
            className="btn btn-icon btn-secondary"
            title="Share with users"
          >
            📤
          </button>
          <button
            onClick={() => {
              if (publicLink) {
                setShowPublicLinkModal(true);
              } else {
                publicLinkMutation.mutate();
              }
            }}
            className="btn btn-icon btn-secondary"
            title={publicLink ? 'View public link' : 'Generate public link'}
            disabled={publicLinkMutation.isLoading}
          >
            🔗
          </button>
        </>
      )}

      {showShareModal && (
        <div
          className="modal-overlay"
          onClick={() => {
            setShowShareModal(false);
            setSelectedUserIds([]);
            setSearchTerm('');
            setShareError(null);
          }}
        >
          <div className="modal-content share-modal" onClick={(e) => e.stopPropagation()}>
            <h3>Share Journey</h3>
            <p>Select users to share with:</p>

            {selectedUserIds.length > 0 && (
              <div className="selected-users">
                <strong>Selected ({selectedUserIds.length}):</strong>
                <div className="selected-users-list">
                  {users
                    ?.filter((u) => selectedUserIds.includes(u.userId))
                    .map((user) => (
                      <span key={user.userId} className="selected-user-tag">
                        {user.name || user.email}
                        <button
                          type="button"
                          onClick={() => toggleUserSelection(user.userId)}
                          className="remove-user-btn"
                          aria-label="Remove user"
                        >
                          ×
                        </button>
                      </span>
                    ))}
                </div>
              </div>
            )}

            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Search users by name, email, or username..."
              className="form-input"
              style={{ marginBottom: '1rem' }}
            />

            {usersLoading ? (
              <div className="loading">Loading users...</div>
            ) : usersError ? (
              <div className="empty-state" style={{ color: 'var(--color-danger)' }}>
                {(usersError as AxiosError<{ detail?: string }>)?.response?.data?.detail ||
                  'Failed to load users. Please check your permissions.'}
              </div>
            ) : filteredUsers.length === 0 ? (
              <div className="empty-state">
                {searchTerm ? 'No users found matching your search.' : 'No users available.'}
              </div>
            ) : (
              <div className="users-list">
                {filteredUsers.map((user) => (
                  <label key={user.userId} className="user-checkbox-item">
                    <input
                      type="checkbox"
                      checked={selectedUserIds.includes(user.userId)}
                      onChange={() => toggleUserSelection(user.userId)}
                    />
                    <div className="user-info">
                      <div className="user-name">{user.name || user.username}</div>
                      <div className="user-email">{user.email}</div>
                    </div>
                  </label>
                ))}
              </div>
            )}

            {shareError && (
              <div
                className="error-message"
                style={{ color: 'var(--color-danger)', marginTop: '0.5rem' }}
              >
                {shareError}
              </div>
            )}

            <div className="modal-actions">
              <button
                onClick={handleShare}
                className="btn btn-primary"
                disabled={shareMutation.isLoading || selectedUserIds.length === 0}
              >
                {shareMutation.isLoading
                  ? 'Sharing...'
                  : `Share with ${selectedUserIds.length} user${selectedUserIds.length !== 1 ? 's' : ''}`}
              </button>
              <button
                onClick={() => {
                  setShowShareModal(false);
                  setSelectedUserIds([]);
                  setSearchTerm('');
                  setShareError(null);
                }}
                className="btn btn-secondary"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {showPublicLinkModal && publicLink && (
        <div className="modal-overlay" onClick={() => setShowPublicLinkModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h3>Public Link</h3>
            <p>Share this link to allow anyone to view the journey:</p>
            <div className="public-link-display">
              <input type="text" value={publicLink} readOnly className="form-input" />
              <button onClick={handleCopyLink} className="btn btn-secondary">
                Copy
              </button>
            </div>
            <div className="modal-actions">
              <button
                onClick={async () => {
                  const confirmed = await confirmDialog.confirm({
                    title: 'Revoke Public Link',
                    message: 'Are you sure you want to revoke this link?',
                    variant: 'warning',
                    confirmText: 'Revoke',
                  });
                  if (confirmed) {
                    revokeLinkMutation.mutate();
                  }
                }}
                className="btn btn-danger"
                disabled={revokeLinkMutation.isLoading}
              >
                {revokeLinkMutation.isLoading ? 'Revoking...' : 'Revoke Link'}
              </button>
              <button onClick={() => setShowPublicLinkModal(false)} className="btn btn-secondary">
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default JourneyActions;
