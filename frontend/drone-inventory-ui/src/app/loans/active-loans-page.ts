import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Loan } from '../api/api-models';
import { problemMessage } from '../api/problem-message';
import { LoanApi } from './loan-api';

// Lists the active loans: Group | Drone | Batteries | Propellers | Loan Date (REQ-007),
// each with a return action (REQ-008, REQ-009).
@Component({
  selector: 'app-active-loans-page',
  imports: [DatePipe],
  templateUrl: './active-loans-page.html',
  styleUrl: './loan-table.css'
})
export class ActiveLoansPage {
  private readonly loanApi = inject(LoanApi);

  protected readonly loans = signal<Loan[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly returnError = signal<string | null>(null);
  protected readonly confirmation = signal<string | null>(null);
  protected readonly returningId = signal<number | null>(null);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.loanApi.list('Active').subscribe({
      next: (loans) => {
        this.loans.set(loans);
        this.error.set(null);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('The active loans could not be loaded.');
        this.loading.set(false);
      }
    });
  }

  // A returned loan is no longer active, so it is removed from this list.
  // It stays stored in the backend and appears in the loan history (REQ-010).
  protected returnLoan(loan: Loan): void {
    this.returningId.set(loan.id);
    this.loanApi.returnLoan(loan.id).subscribe({
      next: () => {
        this.loans.update((loans) => loans.filter((l) => l.id !== loan.id));
        this.returnError.set(null);
        this.confirmation.set(`${loan.droneName} was returned by ${loan.studentGroupName}.`);
        this.returningId.set(null);
      },
      error: (error: unknown) => {
        this.confirmation.set(null);
        this.returnError.set(problemMessage(error));
        this.returningId.set(null);
      }
    });
  }
}
