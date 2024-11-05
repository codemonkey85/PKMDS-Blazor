﻿namespace Pkmds.Web;

public static class Constants
{
    public const string AppTitle = "PKMDS: Pokémon Save Editor for Web";

    public const string SelectedSlotStyle = "border: 4px solid orange; border-radius: 6px;";

    public const long MaxFileSize = 67_108_864L; // 64 bytes in binary

    public const string MaleGenderUnicode = "♂";

    public const string FemaleGenderUnicode = "♀";

    public const string PokeDollarSvg = """
        <svg xmlns="http://www.w3.org/2000/svg"
             viewBox="0 0 1366 2048">
             <path d="M6 1389v-172h152v-144H6V901h152V70h710q171 0 290.5 97.5T1278 414t-119.5 246.5T868 758H352v143h662v172H352v144h662v172H352v147H158v-147H6zm346-803h516q41 0 90-17 35-13 70-41 55-50 55-114 0-66-55-115-35-28-70-41-46-16-90-16H352v344z"/>
        </svg>
        """;
}
