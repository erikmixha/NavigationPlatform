import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQueryClient } from 'react-query';
import { motion, AnimatePresence } from 'framer-motion';
import { MapPin, Plane, ArrowRight, Check } from 'lucide-react';
import { journeyApi, CreateJourneyRequest } from '../services/api';
import { AviationIcon } from '../components/Aviation/AviationIcon';
import { extractErrorMessages, ValidationErrors } from '../utils/errorHandler';
import toast from 'react-hot-toast';
import './CreateJourney.css';

const CreateJourney = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [currentStep, setCurrentStep] = useState(1);
  const [formData, setFormData] = useState<CreateJourneyRequest>({
    startLocation: '',
    startTime: '',
    arrivalLocation: '',
    arrivalTime: '',
    transportType: 'Commercial',
    distanceKm: 0,
  });
  const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({});

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (formData.startTime && formData.arrivalTime) {
      const startTime = new Date(formData.startTime);
      const arrivalTime = new Date(formData.arrivalTime);

      if (arrivalTime <= startTime) {
        errors.arrivalTime = 'Arrival time must be after start time';
      }
    }

    if (formData.distanceKm <= 0) {
      errors.distanceKm = 'Distance must be greater than 0';
    }

    if (!formData.startLocation.trim()) {
      errors.startLocation = 'Start location is required';
    }

    if (!formData.arrivalLocation.trim()) {
      errors.arrivalLocation = 'Arrival location is required';
    }

    setClientErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const validateStep = (step: number): boolean => {
    const errors: Record<string, string> = {};

    if (step === 1) {
      if (!formData.startLocation.trim()) {
        errors.startLocation = 'Start location is required';
      }
      if (!formData.startTime) {
        errors.startTime = 'Start time is required';
      }
    } else if (step === 2) {
      if (!formData.arrivalLocation.trim()) {
        errors.arrivalLocation = 'Arrival location is required';
      }
      if (!formData.arrivalTime) {
        errors.arrivalTime = 'Arrival time is required';
      }
      if (formData.startTime && formData.arrivalTime) {
        const startTime = new Date(formData.startTime);
        const arrivalTime = new Date(formData.arrivalTime);

        if (arrivalTime <= startTime) {
          errors.arrivalTime = 'Arrival time must be after start time';
        }
      }
    } else if (step === 3) {
      if (formData.distanceKm <= 0) {
        errors.distanceKm = 'Distance must be greater than 0';
      }
    }

    setClientErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const createMutation = useMutation((data: CreateJourneyRequest) => journeyApi.create(data), {
    onSuccess: (response) => {
      toast.success('Journey created successfully!');
      const today = new Date().toISOString().split('T')[0];
      queryClient.invalidateQueries('journeys');
      queryClient.invalidateQueries('recent-journeys');
      queryClient.invalidateQueries(['journeys-today', today]);
      navigate(`/journeys/${response.data.id}`);
    },
    onError: (error) => {
      const { message, validationErrors: serverErrors } = extractErrorMessages(error);
      toast.error(message);
      setValidationErrors(serverErrors);
    },
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setClientErrors({});
    setValidationErrors({});

    if (!validateForm()) {
      toast.error('Please fill in all required fields');
      return;
    }

    createMutation.mutate(formData);
  };

  const handleFieldChange = (field: keyof CreateJourneyRequest, value: string | number) => {
    setFormData({ ...formData, [field]: value });
    if (clientErrors[field]) {
      setClientErrors({ ...clientErrors, [field]: '' });
    }
    if (validationErrors[field]) {
      const newErrors = { ...validationErrors };
      delete newErrors[field];
      setValidationErrors(newErrors);
    }
  };

  const nextStep = () => {
    if (validateStep(currentStep)) {
      setCurrentStep(currentStep + 1);
    } else {
      toast.error('Please complete all fields in this step');
    }
  };

  const prevStep = () => {
    setCurrentStep(currentStep - 1);
  };

  const transportTypes = [
    { value: 'Commercial', label: 'Commercial', icon: 'Commercial' },
    { value: 'Cargo', label: 'Cargo', icon: 'Cargo' },
    { value: 'Private', label: 'Private', icon: 'Private' },
    { value: 'Charter', label: 'Charter', icon: 'Charter' },
    { value: 'Military', label: 'Military', icon: 'Military' },
    { value: 'Helicopter', label: 'Helicopter', icon: 'Helicopter' },
    { value: 'Other', label: 'Other', icon: 'Other' },
  ];

  return (
    <div className="page-container create-journey-page">
      <motion.div
        className="journey-form-header"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <h1>Create New Journey</h1>
        <div className="step-indicator">
          <div
            className={`step ${currentStep >= 1 ? 'active' : ''} ${currentStep > 1 ? 'completed' : ''}`}
          >
            {currentStep > 1 ? <Check size={16} /> : '1'}
            <span>Departure</span>
          </div>
          <div className="step-line"></div>
          <div
            className={`step ${currentStep >= 2 ? 'active' : ''} ${currentStep > 2 ? 'completed' : ''}`}
          >
            {currentStep > 2 ? <Check size={16} /> : '2'}
            <span>Arrival</span>
          </div>
          <div className="step-line"></div>
          <div
            className={`step ${currentStep >= 3 ? 'active' : ''} ${currentStep > 3 ? 'completed' : ''}`}
          >
            {currentStep > 3 ? <Check size={16} /> : '3'}
            <span>Details</span>
          </div>
        </div>
      </motion.div>

      <form onSubmit={handleSubmit} className="journey-wizard-form">
        <AnimatePresence mode="wait">
          {/* Step 1: Departure */}
          {currentStep === 1 && (
            <motion.div
              key="step1"
              className="wizard-step"
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -20 }}
            >
              <div className="step-content">
                <div className="step-icon">
                  <MapPin size={32} />
                </div>
                <h2>Where are you departing from?</h2>

                <div className="form-field">
                  <label>Departure Location</label>
                  <input
                    type="text"
                    value={formData.startLocation}
                    onChange={(e) => handleFieldChange('startLocation', e.target.value)}
                    placeholder="e.g., Frankfurt Airport (FRA)"
                    className={clientErrors.startLocation ? 'error' : ''}
                    autoFocus
                  />
                  {clientErrors.startLocation && (
                    <span className="error-message">{clientErrors.startLocation}</span>
                  )}
                </div>

                <div className="form-field">
                  <label>Departure Date & Time</label>
                  <input
                    type="datetime-local"
                    value={formData.startTime}
                    onChange={(e) => handleFieldChange('startTime', e.target.value)}
                    min={new Date().toISOString().slice(0, 16)}
                    className={clientErrors.startTime ? 'error' : ''}
                  />
                  {clientErrors.startTime && (
                    <span className="error-message">{clientErrors.startTime}</span>
                  )}
                </div>
              </div>

              <div className="wizard-actions">
                <button
                  type="button"
                  onClick={() => navigate('/journeys')}
                  className="btn btn-secondary"
                >
                  Cancel
                </button>
                <button type="button" onClick={nextStep} className="btn btn-primary">
                  Next <ArrowRight size={18} />
                </button>
              </div>
            </motion.div>
          )}

          {/* Step 2: Arrival */}
          {currentStep === 2 && (
            <motion.div
              key="step2"
              className="wizard-step"
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -20 }}
            >
              <div className="step-content">
                <div className="step-icon">
                  <Plane size={32} />
                </div>
                <h2>Where are you arriving?</h2>

                <div className="form-field">
                  <label>Arrival Location</label>
                  <input
                    type="text"
                    value={formData.arrivalLocation}
                    onChange={(e) => handleFieldChange('arrivalLocation', e.target.value)}
                    placeholder="e.g., Munich Airport (MUC)"
                    className={clientErrors.arrivalLocation ? 'error' : ''}
                    autoFocus
                  />
                  {clientErrors.arrivalLocation && (
                    <span className="error-message">{clientErrors.arrivalLocation}</span>
                  )}
                </div>

                <div className="form-field">
                  <label>Arrival Date & Time</label>
                  <input
                    type="datetime-local"
                    value={formData.arrivalTime}
                    onChange={(e) => handleFieldChange('arrivalTime', e.target.value)}
                    min={formData.startTime || undefined}
                    className={clientErrors.arrivalTime ? 'error' : ''}
                  />
                  {clientErrors.arrivalTime && (
                    <span className="error-message">{clientErrors.arrivalTime}</span>
                  )}
                </div>
              </div>

              <div className="wizard-actions">
                <button type="button" onClick={prevStep} className="btn btn-secondary">
                  Back
                </button>
                <button type="button" onClick={nextStep} className="btn btn-primary">
                  Next <ArrowRight size={18} />
                </button>
              </div>
            </motion.div>
          )}

          {/* Step 3: Details */}
          {currentStep === 3 && (
            <motion.div
              key="step3"
              className="wizard-step"
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -20 }}
            >
              <div className="step-content">
                <div className="step-icon">
                  <Plane size={32} />
                </div>
                <h2>Journey Details</h2>

                <div className="form-field">
                  <label>Transport Type</label>
                  <div className="transport-grid">
                    {transportTypes.map((type) => (
                      <div
                        key={type.value}
                        className={`transport-option ${formData.transportType === type.value ? 'selected' : ''}`}
                        onClick={() => handleFieldChange('transportType', type.value)}
                      >
                        <AviationIcon
                          type={
                            type.icon as
                              | 'Commercial'
                              | 'Cargo'
                              | 'Private'
                              | 'Charter'
                              | 'Military'
                              | 'Helicopter'
                              | 'Other'
                          }
                          size={24}
                        />
                        <span>{type.label}</span>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="form-field">
                  <label>Distance (km)</label>
                  <input
                    type="number"
                    step="0.1"
                    min="0"
                    value={formData.distanceKm || ''}
                    onChange={(e) =>
                      handleFieldChange('distanceKm', parseFloat(e.target.value) || 0)
                    }
                    placeholder="0.0"
                    className={clientErrors.distanceKm ? 'error' : ''}
                  />
                  {clientErrors.distanceKm && (
                    <span className="error-message">{clientErrors.distanceKm}</span>
                  )}
                </div>

                {/* Journey Summary */}
                <div className="journey-summary">
                  <h3>Journey Summary</h3>
                  <div className="summary-item">
                    <strong>From:</strong> {formData.startLocation || '—'}
                  </div>
                  <div className="summary-item">
                    <strong>To:</strong> {formData.arrivalLocation || '—'}
                  </div>
                  <div className="summary-item">
                    <strong>Departure:</strong>{' '}
                    {formData.startTime ? new Date(formData.startTime).toLocaleString() : '—'}
                  </div>
                  <div className="summary-item">
                    <strong>Arrival:</strong>{' '}
                    {formData.arrivalTime ? new Date(formData.arrivalTime).toLocaleString() : '—'}
                  </div>
                </div>
              </div>

              <div className="wizard-actions">
                <button type="button" onClick={prevStep} className="btn btn-secondary">
                  Back
                </button>
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={createMutation.isLoading}
                >
                  {createMutation.isLoading ? 'Creating...' : 'Create Journey'}
                </button>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </form>
    </div>
  );
};

export default CreateJourney;
