import { useState, useEffect, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, Users, Link as LinkIcon, Check, Copy, Loader2, UserMinus } from 'lucide-react';
import { authApi, journeyApi, User } from '../../services/api';
import { extractErrorMessages } from '../../utils/errorHandler';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import toast from 'react-hot-toast';
import './ShareJourneyModal.css';

interface ShareJourneyModalProps {
  journeyId: string;
  onClose: () => void;
}

export const ShareJourneyModal: React.FC<ShareJourneyModalProps> = ({ journeyId, onClose }) => {
  const confirmDialog = useConfirmDialog();
  const [activeTab, setActiveTab] = useState<'users' | 'link'>('users');
  const [users, setUsers] = useState<User[]>([]);
  const [sharedUserIds, setSharedUserIds] = useState<string[]>([]);
  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);
  const [publicLink, setPublicLink] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [fetchingUsers, setFetchingUsers] = useState(true);

  const fetchUsers = useCallback(async () => {
    try {
      setFetchingUsers(true);
      const response = await authApi.getUsers();
      setUsers(response.data);
    } catch (error) {
      console.error('Failed to load users:', error);
      const { message } = extractErrorMessages(error);
      toast.error(message);
    } finally {
      setFetchingUsers(false);
    }
  }, []);

  const fetchSharedUsers = useCallback(async () => {
    try {
      const response = await journeyApi.getSharedUsers(journeyId);
      setSharedUserIds(response.data);
    } catch (error) {
      console.error('Failed to load shared users:', error);
    }
  }, [journeyId]);

  useEffect(() => {
    fetchUsers();
    fetchSharedUsers();
  }, [fetchUsers, fetchSharedUsers]);

  const handleShareWithUsers = async () => {
    if (selectedUsers.length === 0) {
      toast.error('Please select at least one user');
      return;
    }

    setLoading(true);
    try {
      await journeyApi.share(journeyId, selectedUsers);
      toast.success(`Journey shared with ${selectedUsers.length} user(s)!`);
      await fetchSharedUsers();
      setSelectedUsers([]);
      onClose();
    } catch (error: unknown) {
      console.error('Failed to share journey:', error);
      const { message } = extractErrorMessages(error);
      toast.error(message);
    } finally {
      setLoading(false);
    }
  };

  const handleGeneratePublicLink = async () => {
    setLoading(true);
    try {
      const response = await journeyApi.generatePublicLink(journeyId);
      setPublicLink(response.data.url);
      toast.success('Public link generated!');
    } catch (error: unknown) {
      console.error('Failed to generate link:', error);
      const { message } = extractErrorMessages(error);
      toast.error(message);
    } finally {
      setLoading(false);
    }
  };

  const handleCopyLink = () => {
    if (publicLink) {
      navigator.clipboard.writeText(publicLink);
      toast.success('Link copied to clipboard!');
    }
  };

  const handleRevokeLink = async () => {
    const confirmed = await confirmDialog.confirm({
      title: 'Revoke Public Link',
      message: 'Are you sure you want to revoke this public link?',
      variant: 'warning',
      confirmText: 'Revoke',
    });
    if (!confirmed) {
      return;
    }

    setLoading(true);
    try {
      await journeyApi.revokePublicLink(journeyId);
      setPublicLink(null);
      toast.success('Public link revoked');
    } catch (error: unknown) {
      console.error('Failed to revoke link:', error);
      const { message } = extractErrorMessages(error);
      toast.error(message);
    } finally {
      setLoading(false);
    }
  };

  const handleUnshare = async (userId: string, userName: string) => {
    const confirmed = await confirmDialog.confirm({
      title: 'Unshare Journey',
      message: `Are you sure you want to unshare this journey with ${userName}?`,
      variant: 'warning',
      confirmText: 'Unshare',
    });
    if (!confirmed) {
      return;
    }

    setLoading(true);
    try {
      await journeyApi.unshare(journeyId, userId);
      toast.success(`Journey unshared from ${userName}`);
      await fetchSharedUsers();
    } catch (error: unknown) {
      console.error('Failed to unshare journey:', error);
      const { message } = extractErrorMessages(error);
      toast.error(message);
    } finally {
      setLoading(false);
    }
  };

  const toggleUserSelection = (userId: string) => {
    setSelectedUsers((prev) =>
      prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]
    );
  };

  return (
    <AnimatePresence>
      <div className="modal-backdrop" onClick={onClose}>
        <motion.div
          className="modal-content share-modal"
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          transition={{ duration: 0.2 }}
          onClick={(e) => e.stopPropagation()}
        >
          <div className="modal-header">
            <h2>Share Journey</h2>
            <button className="modal-close" onClick={onClose}>
              <X size={20} />
            </button>
          </div>

          <div className="share-tabs">
            <button
              className={`share-tab ${activeTab === 'users' ? 'active' : ''}`}
              onClick={() => setActiveTab('users')}
            >
              <Users size={18} />
              <span>Share with Users</span>
            </button>
            <button
              className={`share-tab ${activeTab === 'link' ? 'active' : ''}`}
              onClick={() => setActiveTab('link')}
            >
              <LinkIcon size={18} />
              <span>Public Link</span>
            </button>
          </div>

          <div className="share-content">
            {activeTab === 'users' && (
              <div className="share-users-section">
                {fetchingUsers ? (
                  <div className="loading-state">
                    <Loader2 size={32} className="spinner" />
                    <p>Loading users...</p>
                  </div>
                ) : users.length === 0 ? (
                  <p className="empty-state-text">No other users found</p>
                ) : (
                  <>
                    {sharedUserIds.length > 0 && (
                      <div className="shared-users-section">
                        <h4 className="section-title">Currently Shared With</h4>
                        <div className="shared-users-list">
                          {users
                            .filter((user) => sharedUserIds.includes(user.userId))
                            .map((user) => (
                              <div key={user.userId} className="shared-user-item">
                                <div className="user-info">
                                  <div className="user-avatar">
                                    {user.username.charAt(0).toUpperCase()}
                                  </div>
                                  <div className="user-details">
                                    <div className="user-name">{user.username}</div>
                                    <div className="user-email">{user.email}</div>
                                  </div>
                                </div>
                                <button
                                  className="btn btn-sm btn-danger"
                                  onClick={() => handleUnshare(user.userId, user.username)}
                                  disabled={loading}
                                  title="Unshare journey"
                                >
                                  <UserMinus size={16} />
                                  Unshare
                                </button>
                              </div>
                            ))}
                        </div>
                      </div>
                    )}
                    <div className="user-list">
                      <h4 className="section-title">Share With New Users</h4>
                      {users
                        .filter((user) => !sharedUserIds.includes(user.userId))
                        .map((user) => (
                          <label key={user.userId} className="user-item">
                            <input
                              type="checkbox"
                              checked={selectedUsers.includes(user.userId)}
                              onChange={() => toggleUserSelection(user.userId)}
                            />
                            <div className="user-info">
                              <div className="user-avatar">
                                {user.username.charAt(0).toUpperCase()}
                              </div>
                              <div className="user-details">
                                <div className="user-name">{user.username}</div>
                                <div className="user-email">{user.email}</div>
                              </div>
                            </div>
                            {selectedUsers.includes(user.userId) && (
                              <Check size={18} className="check-icon" />
                            )}
                          </label>
                        ))}
                      {users.filter((user) => !sharedUserIds.includes(user.userId)).length ===
                        0 && (
                        <p className="empty-state-text">
                          All available users already have access to this journey
                        </p>
                      )}
                    </div>
                    <button
                      className="btn btn-primary btn-share"
                      onClick={handleShareWithUsers}
                      disabled={loading || selectedUsers.length === 0}
                    >
                      {loading ? (
                        <>
                          <Loader2 size={18} className="spinner" />
                          Sharing...
                        </>
                      ) : (
                        <>Share with {selectedUsers.length} user(s)</>
                      )}
                    </button>
                  </>
                )}
              </div>
            )}

            {activeTab === 'link' && (
              <div className="share-link-section">
                {!publicLink ? (
                  <div className="generate-link-container">
                    <p className="link-description">
                      Generate a public link that anyone can use to view this journey. You can
                      revoke the link at any time.
                    </p>
                    <button
                      className="btn btn-primary"
                      onClick={handleGeneratePublicLink}
                      disabled={loading}
                    >
                      {loading ? (
                        <>
                          <Loader2 size={18} className="spinner" />
                          Generating...
                        </>
                      ) : (
                        <>
                          <LinkIcon size={18} />
                          Generate Public Link
                        </>
                      )}
                    </button>
                  </div>
                ) : (
                  <div className="public-link-display">
                    <div className="public-link-box">
                      <LinkIcon size={18} className="link-icon" />
                      <input
                        type="text"
                        value={publicLink}
                        readOnly
                        className="public-link-input"
                      />
                    </div>
                    <div className="link-actions">
                      <button className="btn btn-secondary" onClick={handleCopyLink}>
                        <Copy size={16} />
                        Copy Link
                      </button>
                      <button
                        className="btn btn-danger"
                        onClick={handleRevokeLink}
                        disabled={loading}
                      >
                        {loading ? 'Revoking...' : 'Revoke Link'}
                      </button>
                    </div>
                    <p className="link-description">
                      ✅ Anyone with this link can view your journey
                    </p>
                  </div>
                )}
              </div>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

export default ShareJourneyModal;
