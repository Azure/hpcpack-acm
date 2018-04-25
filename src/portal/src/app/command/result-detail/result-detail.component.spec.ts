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

class CommandApiStub {
  private counter = 0;

  constructor(public result, public content) {}

  get() {
    return of(this.result);
  }

  getOutput() {
    let res = null;
    switch(this.counter++) {
      case 0:
        res = of({ content: this.content, size: this.content.length, offset: 0 });
        break;
      case 1:
        of({ content: undefined, size: 0, offset: this.content.length });
        break;
    }
    return res;
  }
}

class ApiServiceStub {
  static result = { command: 'TEST COMMAND', nodes: [{ name: 'TEST NODE', state: 'finished' }] };

  static outputContent = 'TEST CONTENT';

  command = new CommandApiStub(ApiServiceStub.result, ApiServiceStub.outputContent);
}

const activatedRouteStub = {
  paramMap: of({ get: () => 1 })
}

//TODO: Enable it when the error is fixed
//Uncaught TypeError: Cannot read property 'nativeElement' of undefined
// at .../src/app/command/result-detail/result-detail.component.ts.ResultDetailComponent.scrollOutputToBottom (_karma_webpack_/webpack:/opt/app/src/app/command/result-detail/result-detail.component.ts:225)
describe('ResultDetailComponent', () => {
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
