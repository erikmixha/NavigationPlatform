import { useParams, Link } from 'react-router-dom';
import { useQuery } from 'react-query';
import { motion } from 'framer-motion';
import { MapPin, Calendar, Clock, Plane, Home } from 'lucide-react';
import { journeyApi, Journey } from '../services/api';
import { AviationIcon } from '../components/Aviation/AviationIcon';
import {
  calculateDuration,
  formatFlightTime,
  formatFlightDate,
  extractAirportCode,
} from '../utils/timeUtils';
import './JourneyDetails.css';

const PublicJourney = () => {
  const { token } = useParams<{ token: string }>();

  const { data, isLoading, error } = useQuery(
    ['public-journey', token],
    () => journeyApi.getByPublicLink(token!),
    {
      enabled: !!token,
      retry: false,
    }
  );

  if (isLoading) {
    return (
      <div className="page-container">
        <div className="loading">Loading journey...</div>
      </div>
    );
  }

  if (error || !data) {
    const status = (error as { response?: { status?: number } })?.response?.status;
    if (status === 410) {
      return (
        <div className="page-container">
          <div className="card">
            <h2>Link Revoked</h2>
            <p>This public link has been revoked and is no longer available.</p>
            <Link to="/" className="btn btn-primary">
              Go to Home
            </Link>
          </div>
        </div>
      );
    }
    return (
      <div className="page-container">
        <div className="card">
          <h2>Journey Not Found</h2>
          <p>The journey you're looking for doesn't exist or the link is invalid.</p>
          <Link to="/" className="btn btn-primary">
            Go to Home
          </Link>
        </div>
      </div>
    );
  }

  const journey = data.data as Journey;
  const duration = calculateDuration(journey.startTime, journey.arrivalTime);
  const departureCode = extractAirportCode(journey.startLocation);
  const arrivalCode = extractAirportCode(journey.arrivalLocation);

  return (
    <div className="page-container journey-details-page">
      {/* Header */}
      <motion.div
        className="journey-details-header"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <div className="public-badge">
          <span className="badge badge-info">🌐 Shared Journey</span>
        </div>
        <Link to="/" className="back-button">
          <Home size={20} />
          Home
        </Link>
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
    </div>
  );
};

export default PublicJourney;
