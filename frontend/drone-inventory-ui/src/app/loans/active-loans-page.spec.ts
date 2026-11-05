import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
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

  async function render(loans: Loan[]) {
    const fixture = TestBed.createComponent(ActiveLoansPage);
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
    expect(header).toEqual(['Group', 'Drone', 'Batteries', 'Propellers', 'Loan Date']);
    expect(rows).toEqual([
      ['Group 1', 'TELLO-02', '3', '4', '19.08.2026'],
      ['Group 3', 'TELLO-07', '4', '8', '19.08.2026']
    ]);
  });

  it('shows a message when no drone is loaned', async () => {
    const page = await render([]);

    expect(page.textContent).toContain('No drone is loaned right now.');
  });
});
