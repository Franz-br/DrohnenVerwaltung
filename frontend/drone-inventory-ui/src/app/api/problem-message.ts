import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './api-models';

// The backend enforces the business rules and explains rejections in a ProblemDetails body
// (400, 404, 409). The frontend only shows that explanation.
export function problemMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as ProblemDetails | null;
    if (problem?.detail) {
      return problem.detail;
    }
  }
  return 'The request failed. Please try again.';
}
