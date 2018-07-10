import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, Directive, Input, Output, EventEmitter } from '@angular/core';
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

@Directive({
  selector: '[appWindowScroll]',
})
class WindowScrollDirectiveStub {
  @Input() currentData: Array<any>;
  @Input() dataSource: any;
  @Input() itemHeight: number;
  @Input() reverse: boolean;
  @Input() updatedSize: number;
  @Input() derelictSize: number;
  @Input() maxPageSize: number;
  @Output() scrollEvent = new EventEmitter();
}

class ApiServiceStub {
  static results = [{ id: 5563, diagnosticTest: { name: 'test', category: 'test' }, progress: 0, state: 'Finished', name: 'test', createdAt: '2018-06-20T10:39:26.0930804+00:00', lastChangedAtAt: '2018-06-20T10:40:24.4964341+00:00' }];

  diag = {
    getDiagsByPage: (lastId: any, count: any, reverse: any) => of(ApiServiceStub.results)
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
        ResultListComponent,
        WindowScrollDirectiveStub
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
  });
});
