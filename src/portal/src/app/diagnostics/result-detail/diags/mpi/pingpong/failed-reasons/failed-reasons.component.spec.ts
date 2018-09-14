import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { FailedReasonsComponent } from './failed-reasons.component';
import { NodesInfoComponent } from '../../../nodes-info/nodes-info.component';
import { MaterialsModule } from '../../../../../../materials.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Directive, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';

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

fdescribe('FailedReasonsComponent', () => {
  let component: FailedReasonsComponent;
  let fixture: ComponentFixture<FailedReasonsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        FailedReasonsComponent,
        NodesInfoComponent,
        RouterLinkDirectiveStub
      ],
      imports: [
        MaterialsModule,
        NoopAnimationsModule,
        FormsModule
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FailedReasonsComponent);
    component = fixture.componentInstance;
    component.failedNodes = {
      'node1': {
        'No result in node mode.': [['node2'], ['node3']],
        'MPI PingPong can not run inside a node with only 1 core.': [['node5']]
      }
    };
    component.failedReasons = [{
      Reason: 'No result',
      NodePairs: [
        ['node1', 'node2'],
        ['node1', 'node3']
      ]
    }, {
      Reason: 'MPI PingPong can not run inside a node with only 1 core.',
      Solution: 'Ignore this failure.',
      Nodes: [
        'node1', 'node5'
      ]

    }];
    component.nodes = ['node1', 'node2', 'node3', 'node4', 'node5'];
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    component.ngOnInit();
    fixture.detectChanges();

    let reason = fixture.nativeElement.querySelectorAll('mat-panel-title div')[0].textContent;
    expect(reason).toEqual(' No result ');

    let solution = fixture.nativeElement.querySelector('.solution-text').textContent;
    expect(solution).toEqual('Ignore this failure.');
  });

  it('should show failure by node', () => {
    fixture.nativeElement.querySelectorAll('.chart-btn')[1].click();
    fixture.detectChanges();
    let nodeReason = fixture.nativeElement.querySelectorAll('mat-panel-title div')[0].textContent;
    expect(nodeReason).toEqual(' No result in node mode. ');
  })
});
