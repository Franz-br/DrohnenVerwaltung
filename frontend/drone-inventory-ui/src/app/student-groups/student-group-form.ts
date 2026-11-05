import { Component, inject, output, signal } from '@angular/core';
import { form, FormField, required, submit } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { StudentGroup } from '../api/api-models';
import { problemMessage } from '../api/problem-message';
import { StudentGroupApi } from './student-group-api';

// Creates a student group with a name (REQ-003). Name rules (not empty, unique) are enforced
// by the backend; the required check here only gives earlier feedback.
@Component({
  selector: 'app-student-group-form',
  imports: [FormField],
  templateUrl: './student-group-form.html',
  styleUrl: './student-group-form.css'
})
export class StudentGroupForm {
  private readonly studentGroupApi = inject(StudentGroupApi);

  readonly created = output<StudentGroup>();

  private readonly model = signal({ name: '' });
  protected readonly groupForm = form(this.model, (path) => {
    required(path.name, { message: 'Please enter a name.' });
  });
  protected readonly serverError = signal<string | null>(null);

  protected save(event: Event): void {
    event.preventDefault();
    submit(this.groupForm, async () => {
      try {
        const group = await firstValueFrom(this.studentGroupApi.create(this.model()));
        this.serverError.set(null);
        this.groupForm().reset({ name: '' });
        this.created.emit(group);
      } catch (error) {
        this.serverError.set(problemMessage(error));
      }
      return undefined;
    });
  }
}
