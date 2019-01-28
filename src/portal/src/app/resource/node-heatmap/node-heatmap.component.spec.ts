import { async, fakeAsync, flush, ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { NodeHeatmapComponent } from './node-heatmap.component'
import { of } from 'rxjs/observable/of';
import { By } from '@angular/platform-browser';
import { ApiService } from '../../services/api.service';
import { Component, Directive, Input } from '@angular/core';
import { MaterialsModule } from '../../materials.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({ selector: 'router-outlet', template: '' })
class RouterOutletStubComponent { }

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

@Component({ selector: 'heatmap-cpu', template: '' })
class CpuComponent {
  @Input()
  activeMode: string;

  @Input()
  nodes: Array<any>;
}


fdescribe('NodeHeatmapComponent', () => {
  let component: NodeHeatmapComponent;
  let fixture: ComponentFixture<NodeHeatmapComponent>;

  let categories = ['disk', 'cpu', 'memory'];
  let mockNodes = {
    values: {
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
      }
    },
    category: "cpu"
  };
  let getCategoriesSpy;
  let getNodesSpy;
  let heatmapService;
  heatmapService = jasmine.createSpyObj('HeatmapService', ['getCategories', 'getMetricInfo']);
  getCategoriesSpy = heatmapService.getCategories.and.returnValue(of(categories));
  getNodesSpy = heatmapService.getMetricInfo.and.returnValue(of(mockNodes));

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
        CpuComponent
      ],
      imports: [
        NoopAnimationsModule,
        MaterialsModule,
        FormsModule,
        ReactiveFormsModule
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

  it('should show select after component initilaized', () => {
    fixture.detectChanges();
    const options = fixture.nativeElement.querySelector('mat-select-value-text');
    const select = fixture.nativeElement.querySelector('mat-select');
    fixture.detectChanges();
    expect(select.textContent).toEqual('Select Category');
  });
});
