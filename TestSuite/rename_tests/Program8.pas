type
  T0 = class
    procedure P(); virtual := exit; // Переименуется T0.P
  end;
  
  T1 = class(T0)
    procedure P(); override := exit; // Переименуется T1.P && T0.P
  end;
  
  T2 = class(T0)
    procedure P(); override := exit; // Переименуется T2.P && T0.P
  end;
  
  T3 = class(T1)
    procedure P(); override := exit; // Переименуется T3.P && T0.P и так далее...
  end;
  
  T4 = class(T2)
    procedure P(); override := exit; // Переименуется T3.P && T0.P и так далее...
  end;
  
  T5 = class(T3)
    procedure P() := exit; // Переименуется T3.P && T0.P и так далее...
  end;
  
  T6 = class(T5)
    procedure P(); override := exit; // Переименуется T3.P && T0.P и так далее...
  end;

begin
end.