import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CommandInputComponent } from './command-input.component';
import { MaterialsModule } from '../../materials.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material';
import { FormsModule } from '@angular/forms';

class MatDialogModuleMock { }

fdescribe('CommandInputComponent', () => {
  let component: CommandInputComponent;
  let fixture: ComponentFixture<CommandInputComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [CommandInputComponent],
      imports: [MaterialsModule, MatDialogModule, NoopAnimationsModule, FormsModule],
      providers: [
        { provide: MatDialogRef, useClass: MatDialogModuleMock },
        { provide: MAT_DIALOG_DATA, useValue: { command: 'test command' } }
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CommandInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    fixture.whenStable().then(() => {
      // ngModel should be available here 
      let text = fixture.nativeElement.querySelector('input').value;
      expect(text).toEqual('test command');
    })
  });
});
