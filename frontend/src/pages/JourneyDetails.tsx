import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { motion } from 'framer-motion';
import {
  ArrowLeft,
  Star,
  Share2,
  Edit2,
  Trash2,
  MapPin,
  Calendar,
  Clock,
  Plane,
} from 'lucide-react';
import { journeyApi, Journey } from '../services/api';
import { AviationIcon } from '../components/Aviation/AviationIcon';
import ShareJourneyModal from '../components/Share/ShareJourneyModal';
import {
  calculateDuration,
  formatFlightTime,
  formatFlightDate,
  extractAirportCode,
} from '../utils/timeUtils';
import { extractErrorMessages } from '../utils/errorHandler';
import { useConfirmDialog } from '../contexts/ConfirmDialogContext';
import toast from 'react-hot-toast';
import './JourneyDetails.css';

const JourneyDetails = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const confirmDialog = useConfirmDialog();
  const queryClient = useQueryClient();
  const [showShareModal, setShowShareModal] = useState(false);

  const { data, isLoading } = useQuery(['journey', id], () =>
    journeyApi.getById(id!).then((res) => res.data)
  );

  const deleteMutation = useMutation(() => journeyApi.delete(id!), {
    onSuccess: () => {
      toast.success('Journey deleted successfully');
      const today = new Date().toISOString().split('T')[0];
      queryClient.invalidateQueries('journeys');
      queryClient.invalidateQueries('recent-journeys');
      queryClient.invalidateQueries(['journeys-today', today]);
      queryClient.invalidateQueries(['journey', id]);
      navigate('/journeys');
    },
    onError: (error) => {
      const { message } = extractErrorMessages(error);
      toast.error(message);
    },
  });

  const favoriteMutation = useMutation(
    (isFavorite: boolean) =>
      isFavorite ? journeyApi.removeFavorite(id!) : journeyApi.addFavorite(id!),
    {
      onSuccess: (_, isFavorite) => {
        toast.success(isFavorite ? 'Removed from favorites' : 'Added to favorites');
        const today = new Date().toISOString().split('T')[0];
        queryClient.invalidateQueries(['journey', id]);
        queryClient.invalidateQueries('journeys');
        queryClient.invalidateQueries(['journeys-today', today]);
      },
      onError: (error) => {
        const { message } = extractErrorMessages(error);
        toast.error(message);
      },
    }
  );

  const handleDelete = async () => {
    const confirmed = await confirmDialog.confirm({
      title: 'Delete Journey',
      message: 'Are you sure you want to delete this journey? This action cannot be undone.',
      variant: 'danger',
      confirmText: 'Delete',
    });
    if (confirmed) {
      deleteMutation.mutate();
    }
  };

  const handleFavoriteToggle = () => {
    if (journey) {
      favoriteMutation.mutate(journey.isFavorite);
    }
  };

  if (isLoading) {
    return (
      <div className="page-container">
        <div className="loading">Loading journey details...</div>
      </div>
    );
  }

  if (!data) {
    return (
      <div className="page-container">
        <div className="card">
          <h2>Journey not found</h2>
          <Link to="/journeys" className="btn btn-primary">
            Back to Journeys
          </Link>
        </div>
      </div>
    );
  }

  const journey = data as Journey;
  const duration = calculateDuration(journey.startTime, journey.arrivalTime);
  const departureCode = extractAirportCode(journey.startLocation);
  const arrivalCode = extractAirportCode(journey.arrivalLocation);

  return (
    <div className="page-container journey-details-page">
      {/* Header with Actions */}
      <motion.div
        className="journey-details-header"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <Link to="/journeys" className="back-button">
          <ArrowLeft size={20} />
          Back
        </Link>

        <div className="header-actions">
          {journey.isShared && (
            <span className="shared-badge-large" title="Shared with you">
              Shared with you
            </span>
          )}
          <button
            className={`action-btn favorite-action-btn ${journey.isFavorite ? 'active' : ''}`}
            onClick={handleFavoriteToggle}
            title={journey.isFavorite ? 'Remove from favorites' : 'Add to favorites'}
          >
            <Star size={20} fill={journey.isFavorite ? 'currentColor' : 'none'} />
          </button>
          {!journey.isShared && (
            <>
              <button
                className="action-btn"
                onClick={() => setShowShareModal(true)}
                title="Share journey"
              >
                <Share2 size={20} />
              </button>
              <Link to={`/journeys/${id}/edit`} className="action-btn" title="Edit journey">
                <Edit2 size={20} />
              </Link>
              <button
                className="action-btn delete"
                onClick={handleDelete}
                disabled={deleteMutation.isLoading}
                title="Delete journey"
              >
                <Trash2 size={20} />
              </button>
            </>
          )}
        </div>
      </motion.div>

      {/* Main Journey Card */}
      <motion.div
        className="journey-details-card"
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.1 }}
      >
        {/* Transport Type Badge */}
        <div className="transport-badge">
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
          <span>{journey.transportType}</span>
        </div>

        {/* Route Display */}
        <div className="route-display">
          <div className="route-location">
            <div className="airport-code-large">{departureCode}</div>
            <div className="location-name-large">{journey.startLocation}</div>
            <div className="datetime-info">
              <Calendar size={16} />
              {formatFlightDate(journey.startTime)}
            </div>
            <div className="datetime-info">
              <Clock size={16} />
              {formatFlightTime(journey.startTime)}
            </div>
          </div>

          <div className="route-path">
            <div className="path-line-large"></div>
            <div className="plane-icon-large">
              <Plane size={32} />
            </div>
            <div className="duration-badge">{duration}</div>
          </div>

          <div className="route-location">
            <div className="airport-code-large">{arrivalCode}</div>
            <div className="location-name-large">{journey.arrivalLocation}</div>
            <div className="datetime-info">
              <Calendar size={16} />
              {formatFlightDate(journey.arrivalTime)}
            </div>
            <div className="datetime-info">
              <Clock size={16} />
              {formatFlightTime(journey.arrivalTime)}
            </div>
          </div>
        </div>

        {/* Journey Stats */}
        <div className="journey-stats-grid">
          <div className="stat-box">
            <MapPin size={24} className="stat-icon" />
            <div className="stat-content">
              <div className="stat-label">Distance</div>
              <div className="stat-value">{journey.distanceKm} km</div>
            </div>
          </div>
          <div className="stat-box">
            <Clock size={24} className="stat-icon" />
            <div className="stat-content">
              <div className="stat-label">Duration</div>
              <div className="stat-value">{duration}</div>
            </div>
          </div>
        </div>
      </motion.div>

      {/* Share Modal */}
      {showShareModal && (
        <ShareJourneyModal journeyId={id!} onClose={() => setShowShareModal(false)} />
      )}
    </div>
  );
};

export default JourneyDetails;
