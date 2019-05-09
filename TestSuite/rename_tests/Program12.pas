type
Person2 = class
  private
    fName1: string;
    fAge: integer;
    a1: Person2;
  end;

type
  Predicate<T> = array of T;
  ProcI<T> = procedure (i: T);
  P<T> = Predicate<T>;
  Pr = Person2;

function FindAll<T>(a: array of T; pred: Predicate<T>): array of T;
begin
  var i := 0;
  var aa: array of integer;
  aa[i] := 5;
  var b := new Pr();
  var c := b.a1;
  var b1: Pr;
  var c1 := b1.a1;
end;

begin
end.