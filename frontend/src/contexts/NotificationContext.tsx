import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { useQueryClient } from 'react-query';

/**
 * Represents a notification received from SignalR or the API.
 */
interface Notification {
  type: string;
  title: string;
  message: string;
  data: Record<string, unknown>;
  timestamp: string;
  JourneyId?: string;
}

/**
 * Notification context type.
 */
interface NotificationContextType {
  notifications: Notification[];
  clearNotifications: () => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

/**
 * Notification provider component that manages SignalR connection and notifications.
 */
export const NotificationProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const queryClient = useQueryClient();

  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection
        .start()
        .then(() => {
          connection.on('ReceiveNotification', (notification: Notification) => {
            console.log('Received notification:', notification);
            setNotifications((prev) => [notification, ...prev]);

            if (
              notification.type &&
              (notification.type.includes('Journey') || notification.JourneyId)
            ) {
              const today = new Date().toISOString().split('T')[0];
              queryClient.invalidateQueries('recent-journeys');
              queryClient.invalidateQueries('journeys');
              queryClient.invalidateQueries(['journeys-today', today]);
              if (notification.JourneyId) {
                queryClient.invalidateQueries(['journey', notification.JourneyId]);
              }
            }
          });
        })
        .catch((error) => console.error('SignalR connection error:', error));

      return () => {
        connection.stop();
      };
    }
  }, [connection, queryClient]);

  /**
   * Clears all notifications from the context.
   */
  const clearNotifications = () => {
    setNotifications([]);
  };

  return (
    <NotificationContext.Provider value={{ notifications, clearNotifications }}>
      {children}
    </NotificationContext.Provider>
  );
};

/**
 * Hook to access the notification context.
 * @throws Error if used outside of NotificationProvider.
 */
// eslint-disable-next-line react-refresh/only-export-components
export const useNotifications = () => {
  const context = useContext(NotificationContext);
  if (context === undefined) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
};
