import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NodesInfoComponent } from './nodes-info.component';
import { Directive, Input } from '@angular/core';

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

fdescribe('NodesInfoComponent', () => {
  let component: NodesInfoComponent;
  let fixture: ComponentFixture<NodesInfoComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [NodesInfoComponent, RouterLinkDirectiveStub]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodesInfoComponent);
    component = fixture.componentInstance;
    component.nodes = ["testNode1", "testNode2"];
    component.badNodes = ["testNode1"];
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let nodes = fixture.nativeElement.querySelectorAll('.node-name');
    expect(nodes.length).toEqual(2);
    let node1 = nodes[0].textContent;
    expect(node1).toEqual('testNode1');
    let node2 = nodes[1].textContent;
    expect(node2).toEqual('testNode2');
  });

  it('should have one bad node', () => {
    let badNodeIcon = fixture.nativeElement.querySelector('.failed i').textContent;
    expect(badNodeIcon).toEqual('block');
    let badNodeName = fixture.nativeElement.querySelector('.failed .node-name').textContent;
    expect(badNodeName).toEqual('testNode1');
  })
});
