import { motion } from 'framer-motion';
import { Star, Share2, Edit2, Trash2, MapPin } from 'lucide-react';
import { Link } from 'react-router-dom';
import clsx from 'clsx';
import { Journey } from '../../services/api';
import { AviationIcon } from './AviationIcon';
import {
  calculateDuration,
  formatFlightTime,
  formatFlightDate,
  extractAirportCode,
} from '../../utils/timeUtils';
import { useConfirmDialog } from '../../contexts/ConfirmDialogContext';
import './FlightCard.css';

interface FlightCardProps {
  journey: Journey;
  onFavoriteToggle?: (journeyId: string, isFavorite: boolean) => void;
  onShare?: (journeyId: string) => void;
  onDelete?: (journeyId: string) => void;
  compact?: boolean;
}

export const FlightCard: React.FC<FlightCardProps> = ({
  journey,
  onFavoriteToggle,
  onShare,
  onDelete,
  compact = false,
}) => {
  const confirmDialog = useConfirmDialog();
  const duration = calculateDuration(journey.startTime, journey.arrivalTime);
  const departureCode = extractAirportCode(journey.startLocation);
  const arrivalCode = extractAirportCode(journey.arrivalLocation);

  const handleFavoriteClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    onFavoriteToggle?.(journey.id, journey.isFavorite);
  };

  const handleShareClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    onShare?.(journey.id);
  };

  const handleDeleteClick = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    const confirmed = await confirmDialog.confirm({
      title: 'Delete Journey',
      message: 'Are you sure you want to delete this journey?',
      variant: 'danger',
      confirmText: 'Delete',
    });
    if (confirmed) {
      onDelete?.(journey.id);
    }
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      whileHover={{ y: -4 }}
      transition={{ duration: 0.2 }}
    >
      <Link to={`/journeys/${journey.id}`} className={clsx('flight-card', { compact })}>
        <div className="flight-card-header">
          <div className="flight-type-badge">
            <AviationIcon
              type={
                journey.transportType as
                  | 'Commercial'
                  | 'Cargo'
                  | 'Private'
                  | 'Charter'
                  | 'Military'
                  | 'Helicopter'
                  | 'Other'
              }
              size={16}
            />
            <span>{journey.transportType}</span>
            {journey.isShared && (
              <span className="shared-badge" title="Shared with you">
                Shared
              </span>
            )}
          </div>
          <button
            className={clsx('favorite-btn', { active: journey.isFavorite })}
            onClick={handleFavoriteClick}
            title={journey.isFavorite ? 'Remove from favorites' : 'Add to favorites'}
          >
            <Star size={20} fill={journey.isFavorite ? 'currentColor' : 'none'} />
          </button>
        </div>

        <div className="flight-route">
          <div className="flight-location">
            <div className="airport-code">{departureCode}</div>
            <div className="location-name">{journey.startLocation}</div>
            <div className="flight-date-time">
              <div className="flight-date">{formatFlightDate(journey.startTime)}</div>
              <div className="flight-time">{formatFlightTime(journey.startTime)}</div>
            </div>
          </div>

          <div className="flight-path">
            <div className="path-line"></div>
            <div className="plane-icon">
              <AviationIcon
                type={
                  journey.transportType as
                    | 'Commercial'
                    | 'Cargo'
                    | 'Private'
                    | 'Charter'
                    | 'Military'
                    | 'Helicopter'
                    | 'Other'
                }
                size={20}
              />
            </div>
            <div className="flight-duration">{duration}</div>
          </div>

          <div className="flight-location">
            <div className="airport-code">{arrivalCode}</div>
            <div className="location-name">{journey.arrivalLocation}</div>
            <div className="flight-date-time">
              <div className="flight-date">{formatFlightDate(journey.arrivalTime)}</div>
              <div className="flight-time">{formatFlightTime(journey.arrivalTime)}</div>
            </div>
          </div>
        </div>

        <div className="flight-card-footer">
          <div className="flight-distance">
            <MapPin size={14} />
            <span>{journey.distanceKm.toFixed(1)} km</span>
          </div>

          <div className="flight-actions">
            {/* Only show share/edit/delete for own journeys */}
            {!journey.isShared && onShare && (
              <button className="action-btn" onClick={handleShareClick} title="Share journey">
                <Share2 size={16} />
              </button>
            )}
            {!journey.isShared && (
              <Link
                to={`/journeys/${journey.id}/edit`}
                className="action-btn"
                title="Edit journey"
                onClick={(e) => e.stopPropagation()}
              >
                <Edit2 size={16} />
              </Link>
            )}
            {!journey.isShared && onDelete && (
              <button
                className="action-btn delete"
                onClick={handleDeleteClick}
                title="Delete journey"
              >
                <Trash2 size={16} />
              </button>
            )}
          </div>
        </div>
      </Link>
    </motion.div>
  );
};

export default FlightCard;
