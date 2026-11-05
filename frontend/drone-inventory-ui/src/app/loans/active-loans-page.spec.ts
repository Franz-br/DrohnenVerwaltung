import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Loan } from '../api/api-models';
import { ActiveLoansPage } from './active-loans-page';

function activeLoan(id: number, group: string, drone: string, batteries: number, propellers: number): Loan {
  return {
    id,
    droneId: id,
    droneName: drone,
    studentGroupId: id,
    studentGroupName: group,
    loanDate: '2026-08-19T08:00:00Z',
    returnDate: null,
    status: 'Active',
    batteries,
    propellers
  };
}

describe('ActiveLoansPage', () => {
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ActiveLoansPage],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  let fixture: ComponentFixture<ActiveLoansPage>;

  async function render(loans: Loan[]) {
    fixture = TestBed.createComponent(ActiveLoansPage);
    httpTesting.expectOne('/api/loans?status=Active').flush(loans);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the example rows from the spec', async () => {
    const page = await render([
      activeLoan(1, 'Group 1', 'TELLO-02', 3, 4),
      activeLoan(2, 'Group 3', 'TELLO-07', 4, 8)
    ]);

    const header = Array.from(page.querySelectorAll('thead th')).map((cell) => cell.textContent?.trim());
    const rows = Array.from(page.querySelectorAll('tbody tr')).map((row) =>
      Array.from(row.querySelectorAll('td')).map((cell) => cell.textContent?.trim())
    );
    expect(header).toEqual(['Group', 'Drone', 'Batteries', 'Propellers', 'Loan Date', 'Actions']);
    expect(rows).toEqual([
      ['Group 1', 'TELLO-02', '3', '4', '19.08.2026', 'Return'],
      ['Group 3', 'TELLO-07', '4', '8', '19.08.2026', 'Return']
    ]);
  });

  it('shows a message when no drone is loaned', async () => {
    const page = await render([]);

    expect(page.textContent).toContain('No drone is loaned right now.');
  });

  function returnButton(page: HTMLElement, label: string) {
    return page.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`)!;
  }

  it('returns a loan and removes it from the list', async () => {
    const page = await render([
      activeLoan(1, 'Group 1', 'TELLO-02', 3, 4),
      activeLoan(2, 'Group 3', 'TELLO-07', 4, 8)
    ]);

    returnButton(page, 'Return TELLO-07 from Group 3').click();
    const request = httpTesting.expectOne('/api/loans/2/return');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    request.flush({ ...activeLoan(2, 'Group 3', 'TELLO-07', 4, 8), status: 'Returned', returnDate: '2026-08-21T10:00:00Z' });
    await fixture.whenStable();

    const drones = Array.from(page.querySelectorAll('tbody tr td:nth-child(2)')).map((cell) => cell.textContent?.trim());
    expect(drones).toEqual(['TELLO-02']);
    expect(page.querySelector('[role="status"]')?.textContent).toContain('TELLO-07 was returned by Group 3.');
  });

  it('shows the backend message when the loan was already returned', async () => {
    const page = await render([activeLoan(2, 'Group 3', 'TELLO-07', 4, 8)]);

    returnButton(page, 'Return TELLO-07 from Group 3').click();
    httpTesting.expectOne('/api/loans/2/return').flush(
      { title: 'Conflict', status: 409, detail: 'Loan 2 has already been returned.' },
      { status: 409, statusText: 'Conflict' }
    );
    await fixture.whenStable();

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('Loan 2 has already been returned.');
    expect(page.querySelectorAll('tbody tr').length).toBe(1);
  });
});
