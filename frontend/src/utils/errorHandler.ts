import { AxiosError } from 'axios';

/**
 * Represents a problem details response from the API.
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

/**
 * Represents validation errors keyed by field name.
 */
export interface ValidationErrors {
  [field: string]: string[];
}

/**
 * Extracts error messages and validation errors from an API error.
 * @param error - The error object (typically an AxiosError).
 * @returns An object containing the error message and validation errors.
 */
export const extractErrorMessages = (
  error: unknown
): {
  message: string;
  validationErrors: ValidationErrors;
} => {
  const axiosError = error as AxiosError<ProblemDetails>;

  if (axiosError.response?.data) {
    const problemDetails = axiosError.response.data;
    const validationErrors: ValidationErrors = problemDetails.errors || {};

    let message = problemDetails.detail || problemDetails.title || 'An error occurred';

    if (Object.keys(validationErrors).length > 0) {
      const errorMessages = Object.entries(validationErrors).flatMap(([field, messages]) =>
        messages.map((msg) => `${field}: ${msg}`)
      );
      message = errorMessages.join(', ');
    }

    return {
      message,
      validationErrors,
    };
  }

  if (axiosError.message) {
    return {
      message: axiosError.message,
      validationErrors: {},
    };
  }

  return {
    message: 'An unexpected error occurred',
    validationErrors: {},
  };
};

const fieldNameMap: Record<string, string> = {
  StartLocation: 'startLocation',
  StartTime: 'startTime',
  ArrivalLocation: 'arrivalLocation',
  ArrivalTime: 'arrivalTime',
  TransportType: 'transportType',
  DistanceKm: 'distanceKm',
};

/**
 * Gets the error message for a specific field from validation errors.
 * @param field - The field name to get the error for.
 * @param validationErrors - The validation errors object.
 * @returns The error message for the field, or undefined if not found.
 */
export const getFieldError = (
  field: string,
  validationErrors: ValidationErrors
): string | undefined => {
  if (validationErrors[field]) {
    return validationErrors[field][0];
  }

  const mappedField = fieldNameMap[field];
  if (mappedField && validationErrors[mappedField]) {
    return validationErrors[mappedField][0];
  }

  const reverseMapped = Object.entries(fieldNameMap).find(([_, value]) => value === field)?.[0];
  if (reverseMapped && validationErrors[reverseMapped]) {
    return validationErrors[reverseMapped][0];
  }

  return undefined;
};
