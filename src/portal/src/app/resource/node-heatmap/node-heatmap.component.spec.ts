import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NodeHeatmapComponent } from './node-heatmap.component';

describe('NodeHeatmapComponent', () => {
  let component: NodeHeatmapComponent;
  let fixture: ComponentFixture<NodeHeatmapComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NodeHeatmapComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeHeatmapComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
