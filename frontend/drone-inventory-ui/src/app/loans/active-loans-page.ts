import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Loan } from '../api/api-models';
import { LoanApi } from './loan-api';

// Lists the active loans: Group | Drone | Batteries | Propellers | Loan Date (REQ-007).
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
}
