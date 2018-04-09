import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, Directive, Input } from '@angular/core';
import { of } from 'rxjs/observable/of';
import { Router, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { FormsModule } from '@angular/forms'
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MaterialsModule } from '../../materials.module';

import { NodeListComponent } from './node-list.component';

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

const routerStub = {
  navigate: () => {},
}

const activatedRouteStub = {
  queryParamMap: of({ get: () => undefined })
}

class ApiServiceStub {
  static nodes = [{ id: 'a node', name: 'a node', state: 'Ok'}]

  node = {
    getAll: () => of(ApiServiceStub.nodes),
  }
}

fdescribe('NodeListComponent', () => {
  let component: NodeListComponent;
  let fixture: ComponentFixture<NodeListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        RouterLinkDirectiveStub,
        NodeListComponent,
      ],
      imports: [
        NoopAnimationsModule,
        FormsModule,
        MaterialsModule,
      ],
      providers: [
        { provide: ApiService, useClass: ApiServiceStub },
        { provide: Router, useValue: routerStub },
        { provide: ActivatedRoute, useValue: activatedRouteStub },
      ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let text = fixture.nativeElement.querySelector('.mat-cell.mat-column-name').textContent;
    expect(text).toContain(ApiServiceStub.nodes[0].name);
    text = fixture.nativeElement.querySelector('.mat-cell.mat-column-state').textContent;
    expect(text).toContain(ApiServiceStub.nodes[0].state);
  });
});
