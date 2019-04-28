type 
  ProcI = procedure (i: integer);
  FunI = function (x,y: integer): integer;
  DayOfWeek = (Mon,Tue,Wed,Thi,Thr,Sat,Sun); 

function Mult(x,y: integer): integer;
begin
  Result := x*y;
end;

var f: FunI := Mult;

begin
  var a: DayOfWeek;
  a := Mon;
  a := DayOfWeek.Wed;
end.