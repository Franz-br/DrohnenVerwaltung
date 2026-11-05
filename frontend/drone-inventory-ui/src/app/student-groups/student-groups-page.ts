import { Component, inject, signal } from '@angular/core';
import { StudentGroup } from '../api/api-models';
import { StudentGroupApi } from './student-group-api';
import { StudentGroupForm } from './student-group-form';

// Lists all student groups and lets the teacher add new ones (REQ-003).
@Component({
  selector: 'app-student-groups-page',
  imports: [StudentGroupForm],
  templateUrl: './student-groups-page.html',
  styleUrl: './student-groups-page.css'
})
export class StudentGroupsPage {
  private readonly studentGroupApi = inject(StudentGroupApi);

  protected readonly groups = signal<StudentGroup[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.studentGroupApi.list().subscribe({
      next: (groups) => {
        this.groups.set(groups);
        this.error.set(null);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('The student groups could not be loaded.');
        this.loading.set(false);
      }
    });
  }
}
