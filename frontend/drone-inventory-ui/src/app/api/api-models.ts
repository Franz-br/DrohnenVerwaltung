// Types of the REST/JSON API of DroneInventory.Api (design D5).
// Dates are ISO 8601 strings in UTC.

export type DroneStatus = 'Available' | 'Loaned';

export interface Drone {
  id: number;
  name: string;
  description: string;
  status: DroneStatus;
}

export interface CreateDroneRequest {
  name: string;
  description: string;
}

export interface StudentGroup {
  id: number;
  name: string;
}

export interface CreateStudentGroupRequest {
  name: string;
}

export type LoanStatus = 'Active' | 'Returned';

export type LoanItemType = 'Battery' | 'Propeller';

export interface LoanItemRequest {
  type: LoanItemType;
  quantity: number;
}

export interface CreateLoanRequest {
  droneId: number;
  studentGroupId: number;
  items: LoanItemRequest[];
}

export interface Loan {
  id: number;
  droneId: number;
  droneName: string;
  studentGroupId: number;
  studentGroupName: string;
  loanDate: string;
  returnDate: string | null;
  status: LoanStatus;
  batteries: number;
  propellers: number;
}

// Error body returned by the backend for 400, 404 and 409 (RFC 7807).
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
}
