import { useState, useEffect, useCallback } from 'react';
import { motion } from 'framer-motion';
import {
  BarChart,
  Bar,
  LineChart,
  Line,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { TrendingUp, Plane, Award, MapPin, Calendar } from 'lucide-react';
import { journeyApi, Journey } from '../services/api';
import { DashboardSkeleton } from '../components/Loading/LoadingSkeleton';
import EmptyState from '../components/Empty/EmptyState';
import { useAuth } from '../contexts/AuthContext';
import { extractErrorMessages } from '../utils/errorHandler';
import toast from 'react-hot-toast';
import './Statistics.css';

interface JourneyStats {
  totalJourneys: number;
  totalDistance: number;
  totalPoints: number;
  favoriteCount: number;
  transportTypeBreakdown: { type: string; count: number; distance: number }[];
  monthlyData: { month: string; distance: number; count: number }[];
  topRoutes: { route: string; count: number }[];
}

const COLORS = ['#05164D', '#F9B000', '#38BDF8', '#10B981', '#8B5CF6', '#EF4444'];

export const Statistics = () => {
  const { user } = useAuth();
  const [stats, setStats] = useState<JourneyStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [timeRange, setTimeRange] = useState<'week' | 'month' | 'year' | 'all'>('all');

  const fetchStatistics = useCallback(async () => {
    try {
      setLoading(true);

      // Calculate date range based on timeRange filter
      const now = new Date();
      let startDate: Date | null = null;

      switch (timeRange) {
        case 'week':
          startDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
          break;
        case 'month':
          startDate = new Date(now.getFullYear(), now.getMonth(), 1);
          break;
        case 'year':
          startDate = new Date(now.getFullYear(), 0, 1);
          break;
        case 'all':
        default:
          startDate = null;
          break;
      }

      const startDateFrom = startDate ? startDate.toISOString() : undefined;
      const startDateTo = undefined;

      const { data } = await journeyApi.getAll(1, 1000, startDateFrom, startDateTo);

      const journeys = data.items.filter((journey: Journey) => {
        const isOwnJourney = journey.userId === user?.userId && !journey.isShared;
        return isOwnJourney;
      });
      const transportTypes = new Map<string, { count: number; distance: number }>();
      const monthlyStats = new Map<string, { distance: number; count: number }>();
      const routes = new Map<string, number>();

      journeys.forEach((journey) => {
        // Transport type breakdown
        const typeData = transportTypes.get(journey.transportType) || { count: 0, distance: 0 };
        transportTypes.set(journey.transportType, {
          count: typeData.count + 1,
          distance: typeData.distance + journey.distanceKm,
        });

        // Monthly data
        const month = new Date(journey.startTime).toLocaleDateString('en-US', {
          month: 'short',
          year: 'numeric',
        });
        const monthData = monthlyStats.get(month) || { distance: 0, count: 0 };
        monthlyStats.set(month, {
          distance: monthData.distance + journey.distanceKm,
          count: monthData.count + 1,
        });

        // Routes
        const route = `${journey.startLocation} → ${journey.arrivalLocation}`;
        routes.set(route, (routes.get(route) || 0) + 1);
      });

      const totalDistance = journeys.reduce((sum, j) => sum + j.distanceKm, 0);
      const favoriteCount = journeys.filter((j) => j.isFavorite).length;

      setStats({
        totalJourneys: journeys.length,
        totalDistance: totalDistance,
        totalPoints: Math.floor(totalDistance * 10),
        favoriteCount,
        transportTypeBreakdown: Array.from(transportTypes.entries()).map(([type, data]) => ({
          type,
          count: data.count,
          distance: data.distance,
        })),
        monthlyData: Array.from(monthlyStats.entries())
          .map(([month, data]) => ({ month, ...data }))
          .slice(-6),
        topRoutes: Array.from(routes.entries())
          .map(([route, count]) => ({ route, count }))
          .sort((a, b) => b.count - a.count)
          .slice(0, 5),
      });
    } catch (error) {
      console.error('Failed to fetch statistics:', error);
      const { message } = extractErrorMessages(error);
      toast.error(message);
    } finally {
      setLoading(false);
    }
  }, [timeRange, user]);

  useEffect(() => {
    fetchStatistics();
  }, [fetchStatistics]);

  if (loading) {
    return <DashboardSkeleton />;
  }

  if (!stats || stats.totalJourneys === 0) {
    return (
      <EmptyState
        title="No Statistics Yet"
        description="Start tracking your journeys to see detailed statistics and insights."
        actionText="Create Your First Journey"
        actionLink="/journeys/new"
        icon="plane"
      />
    );
  }

  return (
    <div className="statistics-page">
      <motion.div
        className="page-header"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <div>
          <h1 className="page-title">
            <TrendingUp size={32} />
            Statistics & Insights
          </h1>
          <p className="page-description">Track your journey performance and trends</p>
        </div>

        <div className="time-range-selector">
          {(['week', 'month', 'year', 'all'] as const).map((range) => (
            <button
              key={range}
              className={`range-btn ${timeRange === range ? 'active' : ''}`}
              onClick={() => setTimeRange(range)}
            >
              {range === 'all' ? 'All Time' : range.charAt(0).toUpperCase() + range.slice(1)}
            </button>
          ))}
        </div>
      </motion.div>

      <motion.div
        className="stats-grid"
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.1 }}
      >
        <div className="stat-card highlight">
          <div className="stat-icon">
            <Plane size={24} />
          </div>
          <div className="stat-content">
            <div className="stat-label">Total Journeys</div>
            <div className="stat-value">{stats.totalJourneys}</div>
          </div>
        </div>

        <div className="stat-card highlight">
          <div className="stat-icon">
            <MapPin size={24} />
          </div>
          <div className="stat-content">
            <div className="stat-label">Total Distance</div>
            <div className="stat-value">{stats.totalDistance.toFixed(1)} km</div>
          </div>
        </div>

        <div className="stat-card highlight">
          <div className="stat-icon">
            <Award size={24} />
          </div>
          <div className="stat-content">
            <div className="stat-label">Total Points</div>
            <div className="stat-value">{stats.totalPoints.toLocaleString()}</div>
          </div>
        </div>

        <div className="stat-card highlight">
          <div className="stat-icon favorite">⭐</div>
          <div className="stat-content">
            <div className="stat-label">Favorite Journeys</div>
            <div className="stat-value">{stats.favoriteCount}</div>
          </div>
        </div>
      </motion.div>

      <div className="charts-grid">
        <motion.div
          className="chart-card"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
        >
          <h3 className="chart-title">
            <Calendar size={20} />
            Monthly Distance Traveled
          </h3>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={stats.monthlyData}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis dataKey="month" stroke="var(--text-secondary)" />
              <YAxis stroke="var(--text-secondary)" />
              <Tooltip
                contentStyle={{
                  background: 'var(--background)',
                  border: '1px solid var(--border)',
                  borderRadius: 'var(--radius)',
                }}
              />
              <Legend />
              <Line
                type="monotone"
                dataKey="distance"
                stroke="var(--lh-primary)"
                strokeWidth={3}
                dot={{ fill: 'var(--lh-accent)', r: 5 }}
                name="Distance (km)"
              />
            </LineChart>
          </ResponsiveContainer>
        </motion.div>

        <motion.div
          className="chart-card"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.3 }}
        >
          <h3 className="chart-title">
            <Plane size={20} />
            Transport Type Distribution
          </h3>
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={stats.transportTypeBreakdown}
                dataKey="count"
                nameKey="type"
                cx="50%"
                cy="50%"
                outerRadius={100}
                label={(entry) => `${entry.type} (${entry.count})`}
              >
                {stats.transportTypeBreakdown.map((_, index) => (
                  <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip
                contentStyle={{
                  background: 'var(--background)',
                  border: '1px solid var(--border)',
                  borderRadius: 'var(--radius)',
                }}
              />
            </PieChart>
          </ResponsiveContainer>
        </motion.div>

        <motion.div
          className="chart-card"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.4 }}
        >
          <h3 className="chart-title">
            <TrendingUp size={20} />
            Distance by Transport Type
          </h3>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={stats.transportTypeBreakdown}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis dataKey="type" stroke="var(--text-secondary)" />
              <YAxis stroke="var(--text-secondary)" />
              <Tooltip
                contentStyle={{
                  background: 'var(--background)',
                  border: '1px solid var(--border)',
                  borderRadius: 'var(--radius)',
                }}
              />
              <Bar dataKey="distance" fill="var(--lh-accent)" name="Distance (km)" />
            </BarChart>
          </ResponsiveContainer>
        </motion.div>

        <motion.div
          className="chart-card"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.5 }}
        >
          <h3 className="chart-title">
            <MapPin size={20} />
            Top Routes
          </h3>
          <div className="top-routes-list">
            {stats.topRoutes.length > 0 ? (
              stats.topRoutes.map((route, index) => (
                <div key={index} className="route-item">
                  <div className="route-rank">{index + 1}</div>
                  <div className="route-info">
                    <div className="route-name">{route.route}</div>
                    <div className="route-count">
                      {route.count} journey{route.count > 1 ? 's' : ''}
                    </div>
                  </div>
                  <div className="route-bar">
                    <div
                      className="route-bar-fill"
                      style={{ width: `${(route.count / stats.topRoutes[0].count) * 100}%` }}
                    />
                  </div>
                </div>
              ))
            ) : (
              <p className="empty-routes">No routes yet</p>
            )}
          </div>
        </motion.div>
      </div>
    </div>
  );
};

export default Statistics;
