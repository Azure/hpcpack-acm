import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CpuComponent } from './cpu.component';
import { MaterialsModule } from '../../../materials.module';
import { Router } from '@angular/router';


const routerStub = {
  navigate: () => { },
}

fdescribe('CpuComponent', () => {
  let component: CpuComponent;
  let fixture: ComponentFixture<CpuComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [CpuComponent],
      imports: [MaterialsModule],
      providers: [
        { provide: Router, useValue: routerStub }
      ]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CpuComponent);
    component = fixture.componentInstance;
    component.activeMode = 'By Node';
    component.nodes = [
      {
        id: 1,
        value: 90
      }
    ];
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();

    let tile = fixture.nativeElement.querySelectorAll(".tile");
    expect(tile.length).toBe(1);
  });
});
