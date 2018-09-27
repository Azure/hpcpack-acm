import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NewCommandComponent } from './new-command.component';
import { MaterialsModule } from '../../materials.module';
import { FormsModule } from '@angular/forms';
import { MatDialogRef } from '@angular/material';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

class MatDialogModuleMock { }

fdescribe('NewCommandComponent', () => {
  let component: NewCommandComponent;
  let fixture: ComponentFixture<NewCommandComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [NewCommandComponent],
      imports: [MaterialsModule, FormsModule, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useClass: MatDialogModuleMock },
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NewCommandComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
