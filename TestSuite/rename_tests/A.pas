unit A;

type
Person3 = class
  private
    fName1: string;
    fAge: integer;
    a1: Person3;
    procedure Print2;
end;

const a8: integer = 5;

procedure Person3.Print2;
begin
  Writeln($'Имя: {a8} Возраст: {fAge}');
end;

begin

end.
