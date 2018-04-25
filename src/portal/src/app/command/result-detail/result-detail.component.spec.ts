import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { of } from 'rxjs/observable/of';
import { _throw } from 'rxjs/observable/throw';

import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { FormsModule } from '@angular/forms'
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MaterialsModule } from '../../materials.module';
import { ResultDetailComponent } from './result-detail.component';

@Component({ selector: 'app-back-button', template: '' })
class BackButtonStubComponent {}

class ApiServiceStub {
  static result = { command: 'TEST COMMAND', nodes: [{ name: 'TEST NODE', state: 'finished' }] };

  static outputContent = 'TEST CONTENT';

  command = {
    get: () => of(ApiServiceStub.result),
    getOutput: () => of(
      { content: ApiServiceStub.outputContent, size: ApiServiceStub.outputContent.length, offset: 0 },
      { size: 0, offset: ApiServiceStub.outputContent.length }
    ),
  }
}

const activatedRouteStub = {
  paramMap: of({ get: () => 1 })
}

fdescribe('ResultDetailComponent', () => {
  let component: ResultDetailComponent;
  let fixture: ComponentFixture<ResultDetailComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        ResultDetailComponent,
        BackButtonStubComponent,
      ],
      imports: [
        NoopAnimationsModule,
        FormsModule,
        MaterialsModule,
      ],
      providers: [
        { provide: ApiService, useClass: ApiServiceStub },
        { provide: ActivatedRoute, useValue: activatedRouteStub },
      ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ResultDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let text = fixture.nativeElement.querySelector('.command').textContent;
    expect(text).toContain(ApiServiceStub.result.command);
    text = fixture.nativeElement.querySelector('.state').textContent;
    expect(text).toContain('Finished');
    text = fixture.nativeElement.querySelector('pre').textContent;
    expect(text).toContain(ApiServiceStub.outputContent);
    text = fixture.nativeElement.querySelector('.mat-cell.mat-column-name').textContent;
    expect(text).toContain(ApiServiceStub.result.nodes[0].name);
    text = fixture.nativeElement.querySelector('.mat-cell.mat-column-state').textContent;
    expect(text).toContain('Finished');
  });
});
