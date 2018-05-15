import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CommandOutputComponent } from './command-output.component';

describe('CommandOutputComponent', () => {
  let component: CommandOutputComponent;
  let fixture: ComponentFixture<CommandOutputComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CommandOutputComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CommandOutputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
