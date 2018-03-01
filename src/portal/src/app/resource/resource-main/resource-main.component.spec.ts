import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ResourceMainComponent } from './resource-main.component';

describe('ResourceMainComponent', () => {
  let component: ResourceMainComponent;
  let fixture: ComponentFixture<ResourceMainComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ResourceMainComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ResourceMainComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
