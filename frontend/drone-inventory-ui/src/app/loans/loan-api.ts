import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateLoanRequest, Loan } from '../api/api-models';

@Service()
export class LoanApi {
  private readonly http = inject(HttpClient);

  create(request: CreateLoanRequest): Observable<Loan> {
    return this.http.post<Loan>('/api/loans', request);
  }
}
