import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { motion } from 'framer-motion';
import { Bell, Check, CheckCheck } from 'lucide-react';
import { format } from 'date-fns';
import toast from 'react-hot-toast';
import { extractErrorMessages } from '../utils/errorHandler';
import { useNotifications } from '../contexts/NotificationContext';
import './Notifications.css';

interface Notification {
  id: string;
  userId: string;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  createdOnUtc: string;
  readOnUtc?: string;
}

const Notifications = () => {
  const queryClient = useQueryClient();
  const { clearNotifications } = useNotifications();
  const [filter, setFilter] = useState<'all' | 'unread' | 'read'>('all');

  // Clear notification badge when page loads
  useEffect(() => {
    clearNotifications();
  }, [clearNotifications]);

  const { data: notifications, isLoading } = useQuery<Notification[]>(
    ['notifications', filter],
    async () => {
      const filterParam = filter === 'all' ? '' : `?isRead=${filter === 'read'}`;
      const response = await fetch(`/api/notifications${filterParam}`, {
        credentials: 'include',
      });
      if (!response.ok) throw new Error('Failed to fetch notifications');
      return response.json();
    }
  );

  const markAsReadMutation = useMutation(
    (id: string) =>
      fetch(`/api/notifications/${id}/read`, {
        method: 'POST',
        credentials: 'include',
      }),
    {
      onSuccess: () => {
        queryClient.invalidateQueries('notifications');
        toast.success('Marked as read');
      },
      onError: (error) => {
        const { message } = extractErrorMessages(error);
        toast.error(message);
      },
    }
  );

  const markAsUnreadMutation = useMutation(
    (id: string) =>
      fetch(`/api/notifications/${id}/unread`, {
        method: 'POST',
        credentials: 'include',
      }),
    {
      onSuccess: () => {
        queryClient.invalidateQueries('notifications');
        toast.success('Marked as unread');
      },
      onError: (error) => {
        const { message } = extractErrorMessages(error);
        toast.error(message);
      },
    }
  );

  const handleToggleRead = (notification: Notification) => {
    if (notification.isRead) {
      markAsUnreadMutation.mutate(notification.id);
    } else {
      markAsReadMutation.mutate(notification.id);
    }
  };

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'DailyGoalAchieved':
        return '🎉';
      case 'JourneyUpdated':
        return '✏️';
      case 'JourneyDeleted':
        return '🗑️';
      default:
        return '📬';
    }
  };

  const notificationList = notifications || [];
  const unreadCount = notificationList.filter((n) => !n.isRead).length;

  return (
    <div className="page-container notifications-page">
      <motion.div
        className="page-header"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <div className="notifications-header-content">
          <div>
            <h1>Notifications</h1>
            <p className="page-description">
              {unreadCount > 0
                ? `You have ${unreadCount} unread notification${unreadCount > 1 ? 's' : ''}`
                : 'All caught up!'}
            </p>
          </div>
          <div className="filter-buttons">
            <button
              className={`filter-btn ${filter === 'all' ? 'active' : ''}`}
              onClick={() => setFilter('all')}
            >
              All
            </button>
            <button
              className={`filter-btn ${filter === 'unread' ? 'active' : ''}`}
              onClick={() => setFilter('unread')}
            >
              Unread
            </button>
            <button
              className={`filter-btn ${filter === 'read' ? 'active' : ''}`}
              onClick={() => setFilter('read')}
            >
              Read
            </button>
          </div>
        </div>
      </motion.div>

      {isLoading ? (
        <div className="loading">Loading notifications...</div>
      ) : notificationList.length === 0 ? (
        <motion.div
          className="card empty-notifications"
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
        >
          <Bell size={64} className="empty-icon" />
          <h2>No notifications</h2>
          <p>
            {filter === 'unread'
              ? 'All caught up! No unread notifications.'
              : filter === 'read'
                ? 'No read notifications yet.'
                : 'You have no notifications yet.'}
          </p>
        </motion.div>
      ) : (
        <motion.div
          className="notifications-list"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
        >
          {notificationList.map((notification, index) => (
            <motion.div
              key={notification.id}
              className={`notification-card ${!notification.isRead ? 'unread' : ''}`}
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ delay: index * 0.05 }}
            >
              <div className="notification-icon">{getNotificationIcon(notification.type)}</div>
              <div className="notification-content">
                <div className="notification-header">
                  <h3>{notification.title}</h3>
                  <span className="notification-time">
                    {format(new Date(notification.createdOnUtc), 'MMM d, yyyy h:mm a')}
                  </span>
                </div>
                <p className="notification-message">{notification.message}</p>
                {notification.type === 'DailyGoalAchieved' && (
                  <div className="notification-badge">
                    <span className="badge badge-success">Achievement</span>
                  </div>
                )}
              </div>
              <div className="notification-actions">
                <button
                  className="notification-action-btn"
                  onClick={() => handleToggleRead(notification)}
                  title={notification.isRead ? 'Mark as unread' : 'Mark as read'}
                >
                  {notification.isRead ? <Check size={18} /> : <CheckCheck size={18} />}
                </button>
              </div>
            </motion.div>
          ))}
        </motion.div>
      )}
    </div>
  );
};

export default Notifications;
