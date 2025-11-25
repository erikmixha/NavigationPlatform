import { describe, it, expect } from 'vitest';
import { AxiosError, InternalAxiosRequestConfig } from 'axios';
import {
  extractErrorMessages,
  getFieldError,
  ProblemDetails,
  ValidationErrors,
} from './errorHandler';

describe('errorHandler', () => {
  describe('extractErrorMessages', () => {
    it('should extract message from problem details', () => {
      const error: AxiosError<ProblemDetails> = {
        response: {
          data: {
            detail: 'Resource not found',
            title: 'Not Found',
          },
          status: 404,
          statusText: 'Not Found',
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed',
      };

      const result = extractErrorMessages(error);
      expect(result.message).toBe('Resource not found');
      expect(result.validationErrors).toEqual({});
    });

    it('should extract validation errors', () => {
      const error: AxiosError<ProblemDetails> = {
        response: {
          data: {
            detail: 'Validation failed',
            errors: {
              startLocation: ['Start location is required'],
              distanceKm: ['Distance must be positive'],
            },
          },
          status: 400,
          statusText: 'Bad Request',
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed',
      };

      const result = extractErrorMessages(error);
      expect(result.message).toContain('startLocation');
      expect(result.message).toContain('distanceKm');
      expect(result.validationErrors).toEqual({
        startLocation: ['Start location is required'],
        distanceKm: ['Distance must be positive'],
      });
    });

    it('should fallback to title when detail is missing', () => {
      const error: AxiosError<ProblemDetails> = {
        response: {
          data: {
            title: 'Bad Request',
          },
          status: 400,
          statusText: 'Bad Request',
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed',
      };

      const result = extractErrorMessages(error);
      expect(result.message).toBe('Bad Request');
    });

    it('should use axios error message when response data is missing', () => {
      const error: AxiosError = {
        message: 'Network Error',
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
      };

      const result = extractErrorMessages(error);
      expect(result.message).toBe('Network Error');
      expect(result.validationErrors).toEqual({});
    });

    it('should return default message for unknown errors', () => {
      const error = {};
      const result = extractErrorMessages(error);
      expect(result.message).toBe('An unexpected error occurred');
      expect(result.validationErrors).toEqual({});
    });

    it('should format validation errors as comma-separated string', () => {
      const error: AxiosError<ProblemDetails> = {
        response: {
          data: {
            errors: {
              field1: ['Error 1', 'Error 2'],
              field2: ['Error 3'],
            },
          },
          status: 400,
          statusText: 'Bad Request',
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed',
      };

      const result = extractErrorMessages(error);
      expect(result.message).toContain('field1: Error 1');
      expect(result.message).toContain('field1: Error 2');
      expect(result.message).toContain('field2: Error 3');
    });
  });

  describe('getFieldError', () => {
    const validationErrors: ValidationErrors = {
      startLocation: ['Start location is required'],
      distanceKm: ['Distance must be positive'],
      StartTime: ['Start time is required'],
    };

    it('should get error for exact field match', () => {
      const error = getFieldError('startLocation', validationErrors);
      expect(error).toBe('Start location is required');
    });

    it('should return first error when multiple errors exist', () => {
      const errors: ValidationErrors = {
        field: ['Error 1', 'Error 2', 'Error 3'],
      };
      const error = getFieldError('field', errors);
      expect(error).toBe('Error 1');
    });

    it('should map PascalCase to camelCase', () => {
      const error = getFieldError('StartTime', validationErrors);
      expect(error).toBe('Start time is required');
    });

    it('should map camelCase to PascalCase', () => {
      const error = getFieldError('startTime', validationErrors);
      expect(error).toBe('Start time is required');
    });

    it('should return undefined for non-existent field', () => {
      const error = getFieldError('nonExistentField', validationErrors);
      expect(error).toBeUndefined();
    });

    it('should handle empty validation errors', () => {
      const error = getFieldError('anyField', {});
      expect(error).toBeUndefined();
    });

    it('should handle all mapped field names', () => {
      const errors: ValidationErrors = {
        StartLocation: ['Error 1'],
        ArrivalLocation: ['Error 2'],
        TransportType: ['Error 3'],
        DistanceKm: ['Error 4'],
      };

      expect(getFieldError('startLocation', errors)).toBe('Error 1');
      expect(getFieldError('arrivalLocation', errors)).toBe('Error 2');
      expect(getFieldError('transportType', errors)).toBe('Error 3');
      expect(getFieldError('distanceKm', errors)).toBe('Error 4');
    });
  });
});
