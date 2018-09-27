import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NewDiagnosticsComponent } from './new-diagnostics.component';
import { of } from 'rxjs';
import { MaterialsModule } from '../../materials.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { ApiService } from '../../services/api.service';
import { MatDialogRef } from '@angular/material';
import { AuthService } from '../../services/auth.service';
import { NoopAnimationsModule, BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { TreeModule } from 'angular-tree-component';

class MatDialogModuleMock {
  public close() {

  }
}

class ApiServiceStub {
  tests = {
    treeData: [
      {
        name: 'Test1',
        children: [
          {
            name: 'test1',
            description: 'test1 description',
            category: 'Test1',
            parameters: [
              {
                name: 'test1 parameter',
                type: 'string',
                description: 'paramter description',
                defaultValue: ''
              }
            ]
          }
        ]
      },
      {
        name: 'Test2',
        children: [
          {
            name: 'test2',
            description: 'test2 description',
            category: 'Test2',
            parameters: [
              {
                name: 'test2 parameter',
                type: 'string',
                description: 'paramter description',
                defaultValue: ''
              }
            ]
          }
        ]
      }
    ],
    rawData: [
      {
        name: 'test1',
        description: 'test1 description',
        category: 'Test1',
        parameters: [
          {
            name: 'test1 parameter',
            type: 'string',
            description: 'paramter description',
            defaultValue: ''
          }
        ]
      },
      {
        name: 'test2',
        description: 'test2 description',
        category: 'Test2',
        parameters: [
          {
            name: 'test2 parameter',
            type: 'string',
            description: 'paramter description',
            defaultValue: ''
          }
        ]
      }
    ]
  };
  diag = {
    getDiagTests: () => of(this.tests)
  }
}

class AuthServiceStub {
  user = {
    name: 'test',
    pwd: ''
  }

  get username() {
    return this.user.name;
  }
}

fdescribe('NewDiagnosticsComponent', () => {
  let component: NewDiagnosticsComponent;
  let fixture: ComponentFixture<NewDiagnosticsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [NewDiagnosticsComponent],
      imports: [MaterialsModule, FormsModule, ReactiveFormsModule, BrowserAnimationsModule, TreeModule],
      providers: [
        { provide: ApiService, useClass: ApiServiceStub },
        { provide: MatDialogRef, useClass: MatDialogModuleMock },
        { provide: AuthService, useClass: AuthServiceStub }
      ],
      schemas: [CUSTOM_ELEMENTS_SCHEMA]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NewDiagnosticsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
