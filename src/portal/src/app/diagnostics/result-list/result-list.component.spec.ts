import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, Directive, Input } from '@angular/core';
import { of } from 'rxjs/observable/of';
import { FormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MaterialsModule } from '../../materials.module';
import { ApiService } from '../../services/api.service';
import { TableSettingsService } from '../../services/table-settings.service';

import { ResultListComponent } from './result-list.component';

@Directive({
  selector: '[routerLink]',
  host: { '(click)': 'onClick()' }
})
class RouterLinkDirectiveStub {
  @Input('routerLink') linkParams: any;
  navigatedTo: any = null;

  onClick() {
    this.navigatedTo = this.linkParams;
  }
}

class ApiServiceStub {
  static results = [{ diagnosticTest: { name: 'test', category: 'test' }, state: 'Finished', name: 'test' }];

  diag = {
    getDiagsByPage: (lastId: any, count: any) => of(ApiServiceStub.results)
  }
}

const tableSettingsStub = {
  load: (key, initVal) => initVal,

  save: (key, val) => undefined
}

fdescribe('ResultListComponent', () => {
  let component: ResultListComponent;
  let fixture: ComponentFixture<ResultListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        RouterLinkDirectiveStub,
        ResultListComponent
      ],
      imports: [
        NoopAnimationsModule,
        FormsModule,
        MaterialsModule
      ],
      providers: [
        { provide: ApiService, useClass: ApiServiceStub },
        { provide: TableSettingsService, useValue: tableSettingsStub }
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ResultListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let text = fixture.nativeElement.querySelector('.mat-cell.mat-column-diagnostic').textContent;
    expect(text).toContain(ApiServiceStub.results[0].diagnosticTest.name);
    text = fixture.nativeElement.querySelector('.mat-cell.mat-column-category').textContent;
    expect(text).toContain(ApiServiceStub.results[0].diagnosticTest.category);
    text = fixture.nativeElement.querySelector('.mat-cell.mat-column-test').textContent;
    expect(text).toContain(ApiServiceStub.results[0].name);
  });
});
