import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { journeyApi, Journey, UpdateJourneyRequest } from '../services/api';
import { extractErrorMessages, getFieldError, ValidationErrors } from '../utils/errorHandler';
import toast from 'react-hot-toast';
import './CreateJourney.css';

const EditJourney = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery(['journey', id], () =>
    journeyApi.getById(id!).then((res) => res.data)
  );

  const [formData, setFormData] = useState<UpdateJourneyRequest>({
    startLocation: '',
    startTime: '',
    arrivalLocation: '',
    arrivalTime: '',
    transportType: 'Commercial',
    distanceKm: 0,
  });
  const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (data) {
      const journey = data as Journey;
      setFormData({
        startLocation: journey.startLocation,
        startTime: new Date(journey.startTime).toISOString().slice(0, 16),
        arrivalLocation: journey.arrivalLocation,
        arrivalTime: new Date(journey.arrivalTime).toISOString().slice(0, 16),
        transportType: journey.transportType,
        distanceKm: journey.distanceKm,
      });
    }
  }, [data]);

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (formData.startTime && formData.arrivalTime) {
      const startTime = new Date(formData.startTime);
      const arrivalTime = new Date(formData.arrivalTime);

      if (arrivalTime <= startTime) {
        errors.arrivalTime = 'Arrival time must be after start time';
      }
    }

    // Validate distance is positive
    if (formData.distanceKm <= 0) {
      errors.distanceKm = 'Distance must be greater than 0';
    }

    // Validate locations are not empty
    if (!formData.startLocation.trim()) {
      errors.startLocation = 'Start location is required';
    }

    if (!formData.arrivalLocation.trim()) {
      errors.arrivalLocation = 'Arrival location is required';
    }

    setClientErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const updateMutation = useMutation((data: UpdateJourneyRequest) => journeyApi.update(id!, data), {
    onSuccess: () => {
      const today = new Date().toISOString().split('T')[0];
      queryClient.invalidateQueries(['journey', id]);
      queryClient.invalidateQueries('journeys');
      queryClient.invalidateQueries('recent-journeys');
      queryClient.invalidateQueries(['journeys-today', today]);
      navigate(`/journeys/${id}`);
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
      return;
    }

    updateMutation.mutate(formData);
  };

  const handleFieldChange = (field: keyof UpdateJourneyRequest, value: string | number) => {
    setFormData({ ...formData, [field]: value });
    // Clear errors for this field when user starts typing
    if (clientErrors[field]) {
      setClientErrors({ ...clientErrors, [field]: '' });
    }
    if (validationErrors[field]) {
      const newErrors = { ...validationErrors };
      delete newErrors[field];
      setValidationErrors(newErrors);
    }
  };

  if (isLoading) {
    return (
      <div className="page-container">
        <div className="loading">Loading...</div>
      </div>
    );
  }

  return (
    <div className="page-container">
      <div className="page-header">
        <h1>Edit Journey</h1>
        <button onClick={() => navigate(-1)} className="btn btn-secondary">
          Cancel
        </button>
      </div>

      <div className="card">
        {updateMutation.isError && (
          <div className="error-alert">
            <strong>Error:</strong> {extractErrorMessages(updateMutation.error).message}
          </div>
        )}

        <form onSubmit={handleSubmit} className="form">
          <div className="form-group">
            <label>Start Location</label>
            <input
              type="text"
              value={formData.startLocation}
              onChange={(e) => handleFieldChange('startLocation', e.target.value)}
              required
              placeholder="Enter start location"
              className={
                clientErrors.startLocation || getFieldError('startLocation', validationErrors)
                  ? 'error'
                  : ''
              }
            />
            {(clientErrors.startLocation || getFieldError('startLocation', validationErrors)) && (
              <span className="error-message">
                {clientErrors.startLocation || getFieldError('startLocation', validationErrors)}
              </span>
            )}
          </div>

          <div className="form-group">
            <label>Start Time</label>
            <input
              type="datetime-local"
              value={formData.startTime}
              onChange={(e) => handleFieldChange('startTime', e.target.value)}
              className={getFieldError('startTime', validationErrors) ? 'error' : ''}
            />
            {getFieldError('startTime', validationErrors) && (
              <span className="error-message">{getFieldError('startTime', validationErrors)}</span>
            )}
          </div>

          <div className="form-group">
            <label>Arrival Location</label>
            <input
              type="text"
              value={formData.arrivalLocation}
              onChange={(e) => handleFieldChange('arrivalLocation', e.target.value)}
              required
              placeholder="Enter arrival location"
              className={
                clientErrors.arrivalLocation || getFieldError('arrivalLocation', validationErrors)
                  ? 'error'
                  : ''
              }
            />
            {(clientErrors.arrivalLocation ||
              getFieldError('arrivalLocation', validationErrors)) && (
              <span className="error-message">
                {clientErrors.arrivalLocation || getFieldError('arrivalLocation', validationErrors)}
              </span>
            )}
          </div>

          <div className="form-group">
            <label>Arrival Time</label>
            <input
              type="datetime-local"
              value={formData.arrivalTime}
              onChange={(e) => handleFieldChange('arrivalTime', e.target.value)}
              min={formData.startTime || undefined}
              className={
                clientErrors.arrivalTime || getFieldError('arrivalTime', validationErrors)
                  ? 'error'
                  : ''
              }
            />
            {(clientErrors.arrivalTime || getFieldError('arrivalTime', validationErrors)) && (
              <span className="error-message">
                {clientErrors.arrivalTime || getFieldError('arrivalTime', validationErrors)}
              </span>
            )}
          </div>

          <div className="form-group">
            <label>Transport Type</label>
            <select
              value={formData.transportType}
              onChange={(e) => handleFieldChange('transportType', e.target.value)}
              className={getFieldError('transportType', validationErrors) ? 'error' : ''}
            >
              <option value="Commercial">Commercial</option>
              <option value="Cargo">Cargo</option>
              <option value="Private">Private</option>
              <option value="Charter">Charter</option>
              <option value="Military">Military</option>
              <option value="Helicopter">Helicopter</option>
              <option value="Other">Other</option>
            </select>
            {getFieldError('transportType', validationErrors) && (
              <span className="error-message">
                {getFieldError('transportType', validationErrors)}
              </span>
            )}
          </div>

          <div className="form-group">
            <label>Distance (km)</label>
            <input
              type="number"
              step="0.1"
              min="0"
              value={formData.distanceKm}
              onChange={(e) => handleFieldChange('distanceKm', parseFloat(e.target.value) || 0)}
              required
              placeholder="0.0"
              className={
                clientErrors.distanceKm || getFieldError('distanceKm', validationErrors)
                  ? 'error'
                  : ''
              }
            />
            {(clientErrors.distanceKm || getFieldError('distanceKm', validationErrors)) && (
              <span className="error-message">
                {clientErrors.distanceKm || getFieldError('distanceKm', validationErrors)}
              </span>
            )}
          </div>

          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={updateMutation.isLoading}>
              {updateMutation.isLoading ? 'Saving...' : 'Save Changes'}
            </button>
            <button type="button" onClick={() => navigate(-1)} className="btn btn-secondary">
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default EditJourney;
