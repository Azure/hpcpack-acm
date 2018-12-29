import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { GoodNodesComponent } from './good-nodes.component';
import { NodesInfoComponent } from '../../../nodes-info/nodes-info.component';
import { MaterialsModule } from '../../../../../../materials.module';
import { Directive, Input } from '@angular/core';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';


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

fdescribe('GoodNodesComponent', () => {
  let component: GoodNodesComponent;
  let fixture: ComponentFixture<GoodNodesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        GoodNodesComponent,
        NodesInfoComponent,
        RouterLinkDirectiveStub
      ],
      imports: [
        MaterialsModule,
        NoopAnimationsModule
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GoodNodesComponent);
    component = fixture.componentInstance;
    component.nodeGroups = [
      ['node1', 'node2', 'node3'],
      ['node4', 'node5']
    ];
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let panels = fixture.nativeElement.querySelectorAll('mat-expansion-panel');
    expect(panels.length).toEqual(2);
  });
});
