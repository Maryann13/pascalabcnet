unit B;

type
Person3 = class
  private
    fName1: string;
    fAge: integer;
    a1: Person3;
    procedure Print2;
  public
    static b2: integer;
end;

const a3: integer = 5;

procedure Person3.Print2;
begin
  Writeln($'Имя: {fName1} Возраст: {fAge}');
end;

begin

end.
