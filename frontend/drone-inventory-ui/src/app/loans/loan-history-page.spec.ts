import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Loan } from '../api/api-models';
import { LoanHistoryPage } from './loan-history-page';

function returnedLoan(id: number, group: string, returnDate: string): Loan {
  return {
    id,
    droneId: 7,
    droneName: 'TELLO-07',
    studentGroupId: id,
    studentGroupName: group,
    loanDate: '2026-08-19T08:00:00Z',
    returnDate,
    status: 'Returned',
    batteries: 4,
    propellers: 8
  };
}

describe('LoanHistoryPage', () => {
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LoanHistoryPage],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  async function render(loans: Loan[]) {
    const fixture = TestBed.createComponent(LoanHistoryPage);
    httpTesting.expectOne('/api/loans?status=Returned').flush(loans);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders returned loans with loan date and return date', async () => {
    const page = await render([
      returnedLoan(1, 'Group 3', '2026-08-21T10:00:00Z'),
      returnedLoan(2, 'Group 1', '2026-09-02T10:00:00Z')
    ]);

    const header = Array.from(page.querySelectorAll('thead th')).map((cell) => cell.textContent?.trim());
    const rows = Array.from(page.querySelectorAll('tbody tr')).map((row) =>
      Array.from(row.querySelectorAll('td')).map((cell) => cell.textContent?.trim())
    );
    expect(header).toEqual(['Group', 'Drone', 'Batteries', 'Propellers', 'Loan Date', 'Return Date']);
    expect(rows).toEqual([
      ['Group 3', 'TELLO-07', '4', '8', '19.08.2026', '21.08.2026'],
      ['Group 1', 'TELLO-07', '4', '8', '19.08.2026', '02.09.2026']
    ]);
  });

  it('shows a message when no loan has been returned', async () => {
    const page = await render([]);

    expect(page.textContent).toContain('No loan has been returned yet.');
  });

  it('shows an error when the history cannot be loaded', async () => {
    const fixture = TestBed.createComponent(LoanHistoryPage);
    httpTesting.expectOne('/api/loans?status=Returned').flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain(
      'could not be loaded'
    );
  });
});
