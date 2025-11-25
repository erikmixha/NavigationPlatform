import './LoadingSkeleton.css';

export const FlightCardSkeleton = () => (
  <div className="flight-card-skeleton">
    <div className="skeleton-header">
      <div className="skeleton skeleton-badge"></div>
      <div className="skeleton skeleton-circle"></div>
    </div>
    <div className="skeleton-route">
      <div className="skeleton skeleton-text-lg"></div>
      <div className="skeleton skeleton-plane"></div>
      <div className="skeleton skeleton-text-lg"></div>
    </div>
    <div className="skeleton-footer">
      <div className="skeleton skeleton-text-sm"></div>
      <div className="skeleton skeleton-text-sm"></div>
    </div>
  </div>
);

export const JourneyListSkeleton = () => (
  <div className="journey-list-skeleton">
    <FlightCardSkeleton />
    <FlightCardSkeleton />
    <FlightCardSkeleton />
    <FlightCardSkeleton />
  </div>
);

export const DashboardSkeleton = () => (
  <div className="dashboard-skeleton">
    <div className="skeleton-card">
      <div className="skeleton skeleton-title"></div>
      <div className="skeleton skeleton-text"></div>
      <div className="skeleton skeleton-text"></div>
      <div className="skeleton skeleton-text"></div>
    </div>
    <div className="skeleton-card">
      <div className="skeleton skeleton-title"></div>
      <div className="skeleton skeleton-text"></div>
      <div className="skeleton skeleton-text"></div>
    </div>
    <div className="skeleton-card">
      <div className="skeleton skeleton-title"></div>
      <div className="skeleton skeleton-chart"></div>
    </div>
  </div>
);

export default FlightCardSkeleton;
