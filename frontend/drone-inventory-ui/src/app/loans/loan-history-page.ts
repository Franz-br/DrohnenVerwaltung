import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Loan } from '../api/api-models';
import { LoanApi } from './loan-api';

// Lists the returned loans, which are kept permanently, with loan and return date (REQ-010).
@Component({
  selector: 'app-loan-history-page',
  imports: [DatePipe],
  templateUrl: './loan-history-page.html',
  styleUrl: './loan-table.css'
})
export class LoanHistoryPage {
  private readonly loanApi = inject(LoanApi);

  protected readonly loans = signal<Loan[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.loanApi.list('Returned').subscribe({
      next: (loans) => {
        this.loans.set(loans);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('The loan history could not be loaded.');
        this.loading.set(false);
      }
    });
  }
}
