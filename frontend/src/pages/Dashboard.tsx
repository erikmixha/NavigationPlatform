import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { motion } from 'framer-motion';
import { Trophy, TrendingUp, MapPin, Plus, BarChart } from 'lucide-react';
import { journeyApi } from '../services/api';
import { FlightCard } from '../components/Aviation/FlightCard';
import { DashboardSkeleton } from '../components/Loading/LoadingSkeleton';
import EmptyState from '../components/Empty/EmptyState';
import AchievementModal from '../components/Achievement/AchievementModal';
import { useAuth } from '../contexts/AuthContext';
import { extractErrorMessages } from '../utils/errorHandler';
import toast from 'react-hot-toast';
import './Dashboard.css';

const Dashboard = () => {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const [showAchievement, setShowAchievement] = useState(false);
  const [achievementData, setAchievementData] = useState({ distance: 0, points: 0 });

  const { data: journeysData, isLoading } = useQuery(
    'recent-journeys',
    () => journeyApi.getAll(1, 5),
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  const favoriteMutation = useMutation(
    ({ journeyId, isFavorite }: { journeyId: string; isFavorite: boolean }) =>
      isFavorite ? journeyApi.removeFavorite(journeyId) : journeyApi.addFavorite(journeyId),
    {
      onSuccess: (_, variables) => {
        toast.success(variables.isFavorite ? 'Removed from favorites' : 'Added to favorites');
        const today = new Date().toISOString().split('T')[0];
        queryClient.invalidateQueries('recent-journeys');
        queryClient.invalidateQueries('journeys');
        queryClient.invalidateQueries(['journey', variables.journeyId]);
        queryClient.invalidateQueries(['journeys-today', today]);
      },
      onError: (error) => {
        const { message } = extractErrorMessages(error);
        toast.error(message);
      },
    }
  );

  const handleFavoriteToggle = (journeyId: string, isFavorite: boolean) => {
    favoriteMutation.mutate({ journeyId, isFavorite });
  };

  const today = new Date().toISOString().split('T')[0];
  const { data: allTodayJourneysData } = useQuery(
    ['journeys-today', today],
    () => journeyApi.getAll(1, 100),
    {
      select: (data) => {
        const todayJourneys = data.data.items.filter((journey) => {
          const journeyDate = new Date(journey.startTime).toISOString().split('T')[0];
          const isOwnJourney = journey.userId === user?.userId && !journey.isShared;
          return journeyDate === today && isOwnJourney;
        });
        return todayJourneys;
      },
      refetchOnWindowFocus: true,
      refetchInterval: 30000,
    }
  );

  const todayJourneys = allTodayJourneysData || [];
  const dailyTotal = todayJourneys.reduce((sum, journey) => sum + journey.distanceKm, 0);
  const dailyGoal = 20.0;
  const hasAchievedDailyGoal = dailyTotal >= dailyGoal;
  const dailyPoints = Math.floor(dailyTotal * 10);

  const [previousTotal, setPreviousTotal] = useState<number | null>(null);
  const [hasShownToday, setHasShownToday] = useState(() => {
    const lastShownDate = localStorage.getItem('achievementShownDate');
    const todayDate = new Date().toISOString().split('T')[0];
    return lastShownDate === todayDate;
  });
  const [isInitialized, setIsInitialized] = useState(false);

  useEffect(() => {
    const lastShownDate = localStorage.getItem('achievementShownDate');
    if (lastShownDate === today) {
      setHasShownToday(true);
    } else {
      setHasShownToday(false);
    }
  }, [today]);

  useEffect(() => {
    const lastShownDate = localStorage.getItem('achievementShownDate');
    if (lastShownDate === today) {
      setHasShownToday(true);
      return;
    }

    if (!isInitialized) {
      setPreviousTotal(dailyTotal);
      setIsInitialized(true);
      return;
    }

    if (previousTotal === null) {
      return;
    }

    const wasBelowGoal = previousTotal < dailyGoal;
    const isNowAboveGoal = dailyTotal >= dailyGoal;

    if (isNowAboveGoal && wasBelowGoal) {
      setAchievementData({ distance: dailyTotal, points: dailyPoints });
      setShowAchievement(true);
      setHasShownToday(true);
      localStorage.setItem('achievementShownDate', today);
    }

    if (previousTotal !== dailyTotal) {
      setPreviousTotal(dailyTotal);
    }
  }, [dailyTotal, dailyGoal, dailyPoints, today, previousTotal, hasShownToday, isInitialized]);

  if (isLoading) {
    return <DashboardSkeleton />;
  }

  return (
    <div className="page-container dashboard-page">
      <motion.div
        className="page-header"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <div>
          <h1 className="page-title">Dashboard</h1>
          <p className="page-description">Welcome back! Here's your journey overview</p>
        </div>
        <div className="header-actions">
          <Link to="/journeys/new" className="btn btn-primary">
            <Plus size={20} />
            New Journey
          </Link>
          <Link to="/statistics" className="btn btn-secondary">
            <BarChart size={20} />
            Statistics
          </Link>
        </div>
      </motion.div>

      {hasAchievedDailyGoal && (
        <motion.div
          className="achievement-banner"
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ delay: 0.2 }}
        >
          <div className="achievement-banner-content">
            <Trophy size={32} className="achievement-banner-icon" />
            <div>
              <h3>Daily Goal Achieved!</h3>
              <p>You've completed {dailyTotal.toFixed(1)} km today. Keep up the great work!</p>
            </div>
          </div>
        </motion.div>
      )}

      <div className="dashboard-grid">
        <motion.div
          className="stats-overview"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
        >
          <div className="stat-card-mini">
            <div className="stat-card-mini-icon">
              <MapPin size={20} />
            </div>
            <div className="stat-card-mini-content">
              <div className="stat-card-mini-label">Today's Distance</div>
              <div className="stat-card-mini-value">{dailyTotal.toFixed(1)} km</div>
            </div>
          </div>

          <div className="stat-card-mini">
            <div className="stat-card-mini-icon">
              <Trophy size={20} />
            </div>
            <div className="stat-card-mini-content">
              <div className="stat-card-mini-label">Daily Goal</div>
              <div className="stat-card-mini-value">{dailyGoal} km</div>
            </div>
          </div>

          <div className="stat-card-mini">
            <div className="stat-card-mini-icon points">⭐</div>
            <div className="stat-card-mini-content">
              <div className="stat-card-mini-label">Today's Points</div>
              <div className="stat-card-mini-value">{dailyPoints}</div>
            </div>
          </div>

          <div className="stat-card-mini">
            <div className="stat-card-mini-icon">
              <TrendingUp size={20} />
            </div>
            <div className="stat-card-mini-content">
              <div className="stat-card-mini-label">Progress</div>
              <div className="stat-card-mini-value">
                {Math.min(100, (dailyTotal / dailyGoal) * 100).toFixed(0)}%
              </div>
            </div>
          </div>
        </motion.div>

        <motion.div
          className="progress-section"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
        >
          <div className="progress-header">
            <span>Daily Goal Progress</span>
            <span className="progress-label">
              {dailyTotal.toFixed(1)} / {dailyGoal} km
            </span>
          </div>
          <div className="progress-bar-container">
            <motion.div
              className="progress-bar-fill"
              initial={{ width: 0 }}
              animate={{ width: `${Math.min(100, (dailyTotal / dailyGoal) * 100)}%` }}
              transition={{ delay: 0.3, duration: 0.8 }}
            />
          </div>
        </motion.div>

        <motion.div
          className="card recent-journeys-section"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.3 }}
        >
          <div className="card-header">
            <h2>Recent Journeys</h2>
            <Link to="/journeys" className="btn btn-sm btn-secondary">
              View All
            </Link>
          </div>
          {journeysData?.data.items.length === 0 ? (
            <EmptyState
              title="No Journeys Yet"
              description="Start your journey tracking today!"
              actionText="Create Journey"
              actionLink="/journeys/new"
              icon="plane"
            />
          ) : (
            <div className="recent-journeys-grid">
              {journeysData?.data.items.map((journey) => (
                <FlightCard
                  key={journey.id}
                  journey={journey}
                  compact
                  onFavoriteToggle={handleFavoriteToggle}
                />
              ))}
            </div>
          )}
        </motion.div>
      </div>

      <AchievementModal
        isOpen={showAchievement}
        onClose={() => setShowAchievement(false)}
        distance={achievementData.distance}
        points={achievementData.points}
        goalDistance={dailyGoal}
      />
    </div>
  );
};

export default Dashboard;
