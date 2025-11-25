import { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import Confetti from 'react-confetti';
import { Trophy, X, Award } from 'lucide-react';
import './AchievementModal.css';

interface AchievementModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  distance: number;
  points?: number;
  goalDistance?: number;
}

export const AchievementModal: React.FC<AchievementModalProps> = ({
  isOpen,
  onClose,
  title = '🎉 Daily Goal Achieved!',
  distance,
  points,
  goalDistance = 20,
}) => {
  const [showConfetti, setShowConfetti] = useState(true);
  const [windowSize, setWindowSize] = useState({
    width: window.innerWidth,
    height: window.innerHeight,
  });

  useEffect(() => {
    if (isOpen) {
      const timer = setTimeout(() => setShowConfetti(false), 5000);
      return () => clearTimeout(timer);
    }
  }, [isOpen]);

  useEffect(() => {
    const handleResize = () => {
      setWindowSize({
        width: window.innerWidth,
        height: window.innerHeight,
      });
    };

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {showConfetti && (
            <Confetti
              width={windowSize.width}
              height={windowSize.height}
              numberOfPieces={200}
              recycle={false}
              colors={['#05164D', '#F9B000', '#38BDF8', '#10B981', '#FFC933']}
            />
          )}
          <div className="modal-backdrop achievement-backdrop" onClick={onClose}>
            <motion.div
              className="achievement-modal"
              initial={{ scale: 0, rotate: -180 }}
              animate={{ scale: 1, rotate: 0 }}
              exit={{ scale: 0, rotate: 180 }}
              transition={{
                type: 'spring',
                stiffness: 200,
                damping: 20,
              }}
              onClick={(e) => e.stopPropagation()}
            >
              <button className="modal-close" onClick={onClose}>
                <X size={20} />
              </button>

              <motion.div
                className="achievement-icon-container"
                initial={{ scale: 0 }}
                animate={{ scale: [0, 1.2, 1] }}
                transition={{ delay: 0.2, duration: 0.5 }}
              >
                <Trophy size={64} className="trophy-icon" />
                <motion.div
                  className="glow-effect"
                  animate={{
                    scale: [1, 1.5, 1],
                    opacity: [0.5, 0.8, 0.5],
                  }}
                  transition={{
                    repeat: Infinity,
                    duration: 2,
                  }}
                />
              </motion.div>

              <motion.h2
                className="achievement-title"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.4 }}
              >
                {title}
              </motion.h2>

              <motion.div
                className="achievement-stats"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.6 }}
              >
                <div className="achievement-stat">
                  <div className="stat-icon">
                    <Award size={24} />
                  </div>
                  <div className="stat-content">
                    <div className="stat-label">Distance Traveled</div>
                    <div className="stat-value">{distance.toFixed(1)} km</div>
                  </div>
                </div>

                {points && (
                  <div className="achievement-stat">
                    <div className="stat-icon points">⭐</div>
                    <div className="stat-content">
                      <div className="stat-label">Points Earned</div>
                      <div className="stat-value">+{points}</div>
                    </div>
                  </div>
                )}
              </motion.div>

              <motion.p
                className="achievement-message"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: 0.8 }}
              >
                Congratulations! You've reached your daily goal of {goalDistance} km! Keep up the
                amazing work! ✈️
              </motion.p>

              <motion.button
                className="btn btn-accent achievement-btn"
                onClick={onClose}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 1 }}
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
              >
                Awesome!
              </motion.button>
            </motion.div>
          </div>
        </>
      )}
    </AnimatePresence>
  );
};

export default AchievementModal;
