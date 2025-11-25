import React from 'react';
import { motion } from 'framer-motion';
import { Plane, Plus, Search, Inbox } from 'lucide-react';
import { Link } from 'react-router-dom';
import './EmptyState.css';

interface EmptyStateProps {
  title: string;
  description: string;
  actionText?: string;
  actionLink?: string;
  onAction?: () => void;
  icon?: 'plane' | 'search' | 'inbox' | React.ReactNode;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  title,
  description,
  actionText,
  actionLink,
  onAction,
  icon = 'plane',
}) => {
  const renderIcon = () => {
    if (typeof icon === 'string') {
      const iconMap: Record<'plane' | 'search' | 'inbox', JSX.Element> = {
        plane: <Plane size={64} className="empty-state-icon-svg" />,
        search: <Search size={64} className="empty-state-icon-svg" />,
        inbox: <Inbox size={64} className="empty-state-icon-svg" />,
      };
      return iconMap[icon as keyof typeof iconMap] || iconMap.plane;
    }
    return icon;
  };

  return (
    <motion.div
      className="empty-state"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      <div className="empty-state-icon">{renderIcon()}</div>
      <h3 className="empty-state-title">{title}</h3>
      <p className="empty-state-description">{description}</p>

      {actionText && (actionLink || onAction) && (
        <div className="empty-state-action">
          {actionLink ? (
            <Link to={actionLink} className="btn btn-primary">
              <Plus size={18} />
              {actionText}
            </Link>
          ) : (
            <button onClick={onAction} className="btn btn-primary">
              <Plus size={18} />
              {actionText}
            </button>
          )}
        </div>
      )}
    </motion.div>
  );
};

export default EmptyState;
