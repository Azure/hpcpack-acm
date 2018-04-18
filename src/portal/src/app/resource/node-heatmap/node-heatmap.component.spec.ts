import { async, fakeAsync, flush, ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { NodeHeatmapComponent } from './node-heatmap.component'
import { of } from 'rxjs/observable/of';
import { By } from '@angular/platform-browser';
import { ApiService } from '../../services/api.service';
import { Component, Directive, Input } from '@angular/core';
import { MaterialsModule } from '../../materials.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

@Component({ selector: 'router-outlet', template: '' })
class RouterOutletStubComponent { }

// @Component({ selector: 'mat-select', template: '' })
// class MatSelectStubComponent { }

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
  navigate: () => { },
}
const activatedRouteStub = {}

describe('NodeHeatmapComponent', () => {
  let component: NodeHeatmapComponent;
  let fixture: ComponentFixture<NodeHeatmapComponent>;

  let categories = ['disk', 'cpu', 'memory'];
  let mockNodes = {
    values: {
      "083868be320a": {},
      "0c53129593a8": {},
      "4916e2c1da8c": {},
      "aabca9fb95e7": {},
      "eb1efeae727b": {},
      "efd3f58626e7": {},
      "evanc6": {},
      "evanclinuxdev": {},
      "testnode1": {
        "_Total": 1.2,
        "_9": 0,
        "_8": 0,
        "_7": 0,
        "_6": 0,
        "_5": 0,
        "_4": 0,
        "_3": 0,
        "_2": 0,
        "_1": 0,
        "_0": 0,
        "_15": 0,
        "_14": 0,
        "_13": 0,
        "_12": 0,
        "_11": 0,
        "_10": 0
      },
      "testnode2": {
        "_Total": 0.6,
        "_9": 0,
        "_8": 0,
        "_7": 0,
        "_6": 0,
        "_5": 0,
        "_4": 0,
        "_3": 0,
        "_2": 0,
        "_1": 0,
        "_0": 0,
        "_15": 0,
        "_14": 0,
        "_13": 0,
        "_12": 9.1,
        "_11": 0,
        "_10": 0
      },
      "testnode3": {},
      "testnode4": {}
    },
    category: "cpu"
  };
  let getCategoriesSpy;
  let getNodesSpy;
  let heatmapService;
  heatmapService = jasmine.createSpyObj('HeatmapService', ['getCategories', 'get']);
  getCategoriesSpy = heatmapService.getCategories.and.returnValue(of(categories));
  getNodesSpy = heatmapService.get.and.returnValue(of(mockNodes));

  const apiServiceStub = {
    get heatmap(): any {
      return heatmapService;
    }
  }

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        NodeHeatmapComponent,
        RouterOutletStubComponent,
        RouterLinkDirectiveStub,
      ],
      imports: [
        NoopAnimationsModule,
        MaterialsModule,
      ],
      providers: [
        { provide: ApiService, useValue: apiServiceStub },
        { provide: Router, useValue: routerStub },
        { provide: ActivatedRoute, useValue: activatedRouteStub },
      ]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeHeatmapComponent);
    component = fixture.componentInstance;

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show select after component initilaized', fakeAsync(() => {
    flush();
    fixture.detectChanges();
    // const options = fixture.debugElement.queryAll(By.css('mat-select-value-text'));
    const select = fixture.nativeElement.querySelector('mat-select');

    expect(select.getAttribute('ng-reflect-value')).toEqual('cpu');
  }));

  it('should display expected nodes after get heamap nodes info', () => {
    fixture.detectChanges();

    const tiles = fixture.debugElement.queryAll(By.css('.tile'));
    // const tiles = fixture.nativeElement.querySelectorAll('.tile');
    // expect(Array.from(tiles).length).toBe(12);
    const testnode1 = tiles[8];

    // expect(testnode1.getAttribute('class')).toContain('low');
    // expect(testnode1.getAttribute('ng-reflect-message')).toEqual('testnode1 : 1.2 %');

    // const evanc6 = tiles[6];
    // expect(evanc6.getAttribute('class')).toContain('high');

  });

});
