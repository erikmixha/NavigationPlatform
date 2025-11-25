import { useState, useEffect, useRef } from 'react';
import { Outlet, Link, useLocation } from 'react-router-dom';
import { Bell, User } from 'lucide-react';
import { useQuery } from 'react-query';
import { useAuth } from '../../contexts/AuthContext';
import { useNotifications } from '../../contexts/NotificationContext';
import './Layout.css';

const Layout = () => {
  const { user, logout } = useAuth();
  const { clearNotifications } = useNotifications();
  const location = useLocation();
  const [showNotifications, setShowNotifications] = useState(false);
  const [showUserMenu, setShowUserMenu] = useState(false);
  const notificationRef = useRef<HTMLDivElement>(null);
  const userMenuRef = useRef<HTMLDivElement>(null);

  const { data: unreadCountData } = useQuery<{ count: number }>(
    ['notifications', 'unread-count'],
    async () => {
      const response = await fetch('/api/notifications/unread/count', {
        credentials: 'include',
      });
      if (!response.ok) throw new Error('Failed to fetch unread count');
      return response.json();
    },
    {
      refetchInterval: 30000,
      enabled: !!user,
    }
  );

  interface NotificationItem {
    id?: string;
    type: string;
    title: string;
    message: string;
    data?: Record<string, unknown>;
    createdOnUtc?: string;
    timestamp?: string;
    isRead?: boolean;
    // Properties that might be in data or directly on the object
    goalDistanceKm?: number;
    totalDistanceKm?: number;
    achievedOnUtc?: string;
    startLocation?: string;
    arrivalLocation?: string;
  }

  const { data: recentNotifications } = useQuery<NotificationItem[]>(
    ['notifications', 'recent'],
    async () => {
      const response = await fetch('/api/notifications?page=1&pageSize=3', {
        credentials: 'include',
      });
      if (!response.ok) throw new Error('Failed to fetch notifications');
      const data = await response.json();
      return Array.isArray(data) ? data : data.items || [];
    },
    {
      refetchInterval: 30000,
      enabled: !!user && showNotifications,
    }
  );

  const unreadCount = unreadCountData?.count ?? 0;

  useEffect(() => {
    if (location.pathname === '/notifications') {
      clearNotifications();
    }
  }, [location.pathname, clearNotifications]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (notificationRef.current && !notificationRef.current.contains(event.target as Node)) {
        setShowNotifications(false);
      }
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setShowUserMenu(false);
      }
    };

    if (showNotifications || showUserMenu) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [showNotifications, showUserMenu]);

  const formatNotification = (notification: NotificationItem) => {
    // Access properties from data if they exist there, otherwise use direct properties
    const data = notification.data || {};
    const goalDistanceKm =
      notification.goalDistanceKm ?? (data.goalDistanceKm as number | undefined);
    const totalDistanceKm =
      notification.totalDistanceKm ?? (data.totalDistanceKm as number | undefined);
    const achievedOnUtc = notification.achievedOnUtc ?? (data.achievedOnUtc as string | undefined);
    const startLocation = notification.startLocation ?? (data.startLocation as string | undefined);
    const arrivalLocation =
      notification.arrivalLocation ?? (data.arrivalLocation as string | undefined);
    const timestamp = notification.timestamp ?? (data.timestamp as string | undefined);
    const createdOnUtc = notification.createdOnUtc ?? (data.createdOnUtc as string | undefined);

    if (notification.title && notification.message) {
      return {
        title: notification.title,
        message: notification.message,
        timestamp: createdOnUtc || timestamp,
      };
    }

    if (notification.type === 'DailyGoalAchieved') {
      return {
        title: '🎉 Daily Goal Achieved!',
        message: `Congratulations! You've achieved your daily goal of ${goalDistanceKm} km with a total of ${totalDistanceKm} km today!`,
        timestamp: timestamp || achievedOnUtc || createdOnUtc,
      };
    }
    if (notification.type === 'JourneyUpdated') {
      return {
        title: '✏️ Journey Updated',
        message: `${startLocation} → ${arrivalLocation} has been updated.`,
        timestamp: timestamp || createdOnUtc,
      };
    }
    if (notification.type === 'JourneyDeleted') {
      return {
        title: '🗑️ Journey Deleted',
        message: `Journey from ${startLocation} to ${arrivalLocation} has been deleted.`,
        timestamp: timestamp || createdOnUtc,
      };
    }
    return {
      title: notification.title || 'Notification',
      message: notification.message || JSON.stringify(notification),
      timestamp: createdOnUtc || timestamp,
    };
  };

  return (
    <div className="layout">
      <header className="header">
        <div className="container">
          <div className="header-content">
            <h1 className="logo">NavPlat</h1>
            <nav className="nav">
              {user?.roles.includes('Admin') ? (
                <>
                  <Link to="/admin">Admin Panel</Link>
                </>
              ) : (
                <>
                  <Link to="/dashboard">Dashboard</Link>
                  <Link to="/journeys">Journeys</Link>
                  <Link to="/statistics">Statistics</Link>
                </>
              )}
              {user?.roles.includes('Admin') && user?.roles.includes('User') && (
                <>
                  <Link to="/dashboard">Dashboard</Link>
                  <Link to="/journeys">Journeys</Link>
                  <Link to="/statistics">Statistics</Link>
                </>
              )}
            </nav>
            <div className="header-actions">
              <div className="notification-container" ref={notificationRef}>
                <button
                  className="header-icon-btn notification-btn"
                  onClick={() => setShowNotifications(!showNotifications)}
                  aria-label="Notifications"
                >
                  <Bell size={20} />
                  {unreadCount > 0 && <span className="notification-count">{unreadCount}</span>}
                </button>
                {showNotifications && (
                  <div className="notification-popup">
                    <div className="notification-popup-header">
                      <h3>Notifications</h3>
                      <Link
                        to="/notifications"
                        className="view-all-link"
                        onClick={() => {
                          clearNotifications();
                          setShowNotifications(false);
                        }}
                      >
                        View All
                      </Link>
                    </div>
                    <div className="notification-popup-content">
                      {!recentNotifications || recentNotifications.length === 0 ? (
                        <p className="notification-empty">No notifications</p>
                      ) : (
                        recentNotifications.map((notification, index) => {
                          const formatted = formatNotification(notification);
                          const isUnread = !notification.isRead;
                          const timestamp = formatted.timestamp || notification.createdOnUtc;
                          return (
                            <div
                              key={notification.id || index}
                              className={`notification-popup-item ${isUnread ? 'unread' : ''}`}
                            >
                              <div className="notification-popup-item-header">
                                <div
                                  style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}
                                >
                                  <strong>{formatted.title}</strong>
                                  {isUnread && <span className="new-badge">New</span>}
                                </div>
                                {timestamp && (
                                  <span className="notification-time">
                                    {new Date(timestamp).toLocaleTimeString([], {
                                      hour: '2-digit',
                                      minute: '2-digit',
                                    })}
                                  </span>
                                )}
                              </div>
                              <p>{formatted.message || notification.message}</p>
                            </div>
                          );
                        })
                      )}
                    </div>
                  </div>
                )}
              </div>
              <div className="user-menu-container" ref={userMenuRef}>
                <button
                  className="user-info-btn"
                  onClick={() => setShowUserMenu(!showUserMenu)}
                  aria-label="User menu"
                >
                  <User size={16} />
                  <span className="user-name">{user?.name || 'Test User'}</span>
                </button>
                {showUserMenu && (
                  <div className="user-menu-popup">
                    <div className="user-menu-header">
                      <User size={20} />
                      <div>
                        <div className="user-menu-name">{user?.name || 'Test User'}</div>
                        <div className="user-menu-email">{user?.email || ''}</div>
                      </div>
                    </div>
                    <div className="user-menu-divider"></div>
                    <button
                      onClick={() => {
                        logout();
                        setShowUserMenu(false);
                      }}
                      className="user-menu-item"
                    >
                      Logout
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </header>
      <main className="main">
        <div className="container">
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default Layout;
