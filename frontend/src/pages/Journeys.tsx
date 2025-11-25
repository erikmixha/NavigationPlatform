import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { motion } from 'framer-motion';
import { Plus, Plane } from 'lucide-react';
import { journeyApi } from '../services/api';
import { FlightCard } from '../components/Aviation/FlightCard';
import { JourneyListSkeleton } from '../components/Loading/LoadingSkeleton';
import EmptyState from '../components/Empty/EmptyState';
import ShareJourneyModal from '../components/Share/ShareJourneyModal';
import { extractErrorMessages } from '../utils/errorHandler';
import toast from 'react-hot-toast';
import './Journeys.css';

const Journeys = () => {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [shareJourneyId, setShareJourneyId] = useState<string | null>(null);
  const pageSize = 20;

  const { data, isLoading } = useQuery(
    ['journeys', page],
    () => journeyApi.getAll(page, pageSize),
    {
      refetchInterval: 30000,
      refetchOnWindowFocus: true,
    }
  );

  const deleteMutation = useMutation((id: string) => journeyApi.delete(id), {
    onSuccess: () => {
      toast.success('Journey deleted successfully');
      const today = new Date().toISOString().split('T')[0];
      queryClient.invalidateQueries('journeys');
      queryClient.invalidateQueries('recent-journeys');
      queryClient.invalidateQueries(['journeys-today', today]);
    },
    onError: (error) => {
      const { message } = extractErrorMessages(error);
      toast.error(message);
    },
  });

  const favoriteMutation = useMutation(
    ({ journeyId, isFavorite }: { journeyId: string; isFavorite: boolean }) =>
      isFavorite ? journeyApi.removeFavorite(journeyId) : journeyApi.addFavorite(journeyId),
    {
      onSuccess: (_, variables) => {
        toast.success(variables.isFavorite ? 'Removed from favorites' : 'Added to favorites');
        queryClient.invalidateQueries('journeys');
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

  const handleShare = (journeyId: string) => {
    setShareJourneyId(journeyId);
  };

  const handleDelete = (journeyId: string) => {
    deleteMutation.mutate(journeyId);
  };

  if (isLoading) {
    return (
      <div className="page-container">
        <JourneyListSkeleton />
      </div>
    );
  }

  const journeys = data?.data.items || [];
  const totalPages = Math.ceil((data?.data.totalCount || 0) / pageSize);

  return (
    <div className="page-container journeys-page">
      <motion.div
        className="page-header"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <div>
          <h1 className="page-title">
            <Plane size={32} />
            My Journeys
          </h1>
          <p className="page-description">Track and manage all your travel journeys</p>
        </div>
        <Link to="/journeys/new" className="btn btn-primary">
          <Plus size={20} />
          Create Journey
        </Link>
      </motion.div>

      {journeys.length === 0 ? (
        <EmptyState
          title="No Journeys Yet"
          description="Start tracking your travels by creating your first journey. Every journey brings you closer to your goals!"
          actionText="Create Your First Journey"
          actionLink="/journeys/new"
          icon="plane"
        />
      ) : (
        <>
          <div className="journeys-grid">
            {journeys.map((journey) => (
              <FlightCard
                key={journey.id}
                journey={journey}
                onFavoriteToggle={handleFavoriteToggle}
                onShare={handleShare}
                onDelete={handleDelete}
              />
            ))}
          </div>

          {totalPages > 1 && (
            <motion.div
              className="pagination"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ delay: 0.2 }}
            >
              <button
                className="btn btn-secondary"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
              >
                Previous
              </button>
              <span className="pagination-info">
                Page {page} of {totalPages}
              </span>
              <button
                className="btn btn-secondary"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
              >
                Next
              </button>
            </motion.div>
          )}
        </>
      )}

      {shareJourneyId && (
        <ShareJourneyModal journeyId={shareJourneyId} onClose={() => setShareJourneyId(null)} />
      )}
    </div>
  );
};

export default Journeys;
