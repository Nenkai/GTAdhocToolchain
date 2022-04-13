﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core
{
    public enum Keycodes
    {
        KEY_SPACE = 0X0020,  /* U+0020 SPACE */
        KEY_EXCLAM = 0X0021,  /* U+0021 EXCLAMATION MARK */
        KEY_QUOTEDBL = 0X0022,  /* U+0022 QUOTATION MARK */
        KEY_NUMBERSIGN = 0X0023,  /* U+0023 NUMBER SIGN */
        KEY_DOLLAR = 0X0024,  /* U+0024 DOLLAR SIGN */
        KEY_PERCENT = 0X0025,  /* U+0025 PERCENT SIGN */
        KEY_AMPERSAND = 0X0026,  /* U+0026 AMPERSAND */
        KEY_APOSTROPHE = 0X0027,  /* U+0027 APOSTROPHE */
        KEY_PARENLEFT = 0X0028,  /* U+0028 LEFT PARENTHESIS */
        KEY_PARENRIGHT = 0X0029,  /* U+0029 RIGHT PARENTHESIS */
        KEY_ASTERISK = 0X002A,  /* U+002A ASTERISK */
        KEY_PLUS = 0X002B,  /* U+002B PLUS SIGN */
        KEY_COMMA = 0X002C,  /* U+002C COMMA */
        KEY_MINUS = 0X002D,  /* U+002D HYPHEN-MINUS */
        KEY_PERIOD = 0X002E,  /* U+002E FULL STOP */
        KEY_SLASH = 0X002F,  /* U+002F SOLIDUS */

        KEY_0 = 0X30,
        KEY_1 = 0X31,
        KEY_2 = 0X32,
        KEY_3 = 0X33,
        KEY_4 = 0X34,
        KEY_5 = 0X35,
        KEY_6 = 0X36,
        KEY_7 = 0X37,
        KEY_8 = 0X38,
        KEY_9 = 0X39,
        KEY_COLON = 0X003A,  /* U+003A COLON */
        KEY_SEMICOLON = 0X003B,  /* U+003B SEMICOLON */
        KEY_LESS = 0X003C,  /* U+003C LESS-THAN SIGN */
        KEY_EQUAL = 0X003D,  /* U+003D EQUALS SIGN */
        KEY_GREATER = 0X003E,  /* U+003E GREATER-THAN SIGN */
        KEY_QUESTION = 0X003F,  /* U+003F QUESTION MARK */
        KEY_AT = 0X0040,  /* U+0040 COMMERCIAL AT */
        KEY_A = 0X41,
        KEY_B = 0X42,
        KEY_C = 0X43,
        KEY_D = 0X44,
        KEY_E = 0X45,
        KEY_F = 0X46,
        KEY_G = 0X47,
        KEY_H = 0X48,
        KEY_I = 0X49,
        KEY_J = 0X4A,
        KEY_K = 0X4B,
        KEY_L = 0X4C,
        KEY_M = 0X4D,
        KEY_N = 0X4E,
        KEY_O = 0X4F,
        KEY_P = 0X50,
        KEY_Q = 0X51,
        KEY_R = 0X52,
        KEY_S = 0X53,
        KEY_T = 0X54,
        KEY_U = 0X55,
        KEY_V = 0X56,
        KEY_W = 0X57,
        KEY_X = 0X58,
        KEY_Y = 0X59,
        KEY_Z = 0X5A,
        KEY_BRACKETLEFT = 0X005B,  /* U+005B LEFT SQUARE BRACKET */
        KEY_BACKSLASH = 0X005C,  /* U+005C REVERSE SOLIDUS */
        KEY_BRACKETRIGHT = 0X005D,  /* U+005D RIGHT SQUARE BRACKET */
        KEY_ASCIICIRCUM = 0X005E,  /* U+005E CIRCUMFLEX ACCENT */
        KEY_UNDERSCORE = 0X005F,  /* U+005F LOW LINE */
        KEY_GRAVE = 0X0060,  /* U+0060 GRAVE ACCENT */
        KEY_a = 0x0061,  /* U+0061 LATIN SMALL LETTER A */
        KEY_b = 0x0062,  /* U+0062 LATIN SMALL LETTER B */
        KEY_c = 0x0063,  /* U+0063 LATIN SMALL LETTER C */
        KEY_d = 0x0064,  /* U+0064 LATIN SMALL LETTER D */
        KEY_e = 0x0065,  /* U+0065 LATIN SMALL LETTER E */
        KEY_f = 0x0066,  /* U+0066 LATIN SMALL LETTER F */
        KEY_g = 0x0067,  /* U+0067 LATIN SMALL LETTER G */
        KEY_h = 0x0068,  /* U+0068 LATIN SMALL LETTER H */
        KEY_i = 0x0069,  /* U+0069 LATIN SMALL LETTER I */
        KEY_j = 0x006a,  /* U+006A LATIN SMALL LETTER J */
        KEY_k = 0x006b,  /* U+006B LATIN SMALL LETTER K */
        KEY_l = 0x006c,  /* U+006C LATIN SMALL LETTER L */
        KEY_m = 0x006d,  /* U+006D LATIN SMALL LETTER M */
        KEY_n = 0x006e,  /* U+006E LATIN SMALL LETTER N */
        KEY_o = 0x006f,  /* U+006F LATIN SMALL LETTER O */
        KEY_p = 0x0070,  /* U+0070 LATIN SMALL LETTER P */
        KEY_q = 0x0071,  /* U+0071 LATIN SMALL LETTER Q */
        KEY_r = 0x0072,  /* U+0072 LATIN SMALL LETTER R */
        KEY_s = 0x0073,  /* U+0073 LATIN SMALL LETTER S */
        KEY_t = 0x0074,  /* U+0074 LATIN SMALL LETTER T */
        KEY_u = 0x0075,  /* U+0075 LATIN SMALL LETTER U */
        KEY_v = 0x0076,  /* U+0076 LATIN SMALL LETTER V */
        KEY_w = 0x0077,  /* U+0077 LATIN SMALL LETTER W */
        KEY_x = 0x0078,  /* U+0078 LATIN SMALL LETTER X */
        KEY_y = 0x0079,  /* U+0079 LATIN SMALL LETTER Y */
        KEY_z = 0x007a,  /* U+007A LATIN SMALL LETTER Z */
        KEY_BRACELEFT = 0X007B,  /* U+007B LEFT CURLY BRACKET */
        KEY_BAR = 0X007C,  /* U+007C VERTICAL LINE */
        KEY_BRACERIGHT = 0X007D,  /* U+007D RIGHT CURLY BRACKET */
        KEY_ASCIITILDE = 0X007E,  /* U+007E TILDE */
        KEY_NOBREAKSPACE = 0X00A0,  /* U+00A0 NO-BREAK SPACE */
        KEY_EXCLAMDOWN = 0X00A1,  /* U+00A1 INVERTED EXCLAMATION MARK */
        KEY_CENT = 0X00A2,  /* U+00A2 CENT SIGN */
        KEY_STERLING = 0X00A3,  /* U+00A3 POUND SIGN */
        KEY_CURRENCY = 0X00A4,  /* U+00A4 CURRENCY SIGN */
        KEY_YEN = 0X00A5,  /* U+00A5 YEN SIGN */
        KEY_BROKENBAR = 0X00A6,  /* U+00A6 BROKEN BAR */
        KEY_SECTION = 0X00A7,  /* U+00A7 SECTION SIGN */
        KEY_DIAERESIS = 0X00A8,  /* U+00A8 DIAERESIS */
        KEY_COPYRIGHT = 0X00A9,  /* U+00A9 COPYRIGHT SIGN */
        KEY_ORDFEMININE = 0X00AA,  /* U+00AA FEMININE ORDINAL INDICATOR */
        KEY_GUILLEMOTLEF = 0X00AB,  /* U+00AB LEFT-POINTING DOUBLE ANGLE QUOTATION MARK */
        KEY_NOTSIGN = 0X00AC,  /* U+00AC NOT SIGN */
        KEY_HYPHEN = 0X00AD,  /* U+00AD SOFT HYPHEN */
        KEY_REGISTERED = 0X00AE,  /* U+00AE REGISTERED SIGN */
        KEY_MACRON = 0X00AF,  /* U+00AF MACRON */
        KEY_DEGREE = 0X00B0,  /* U+00B0 DEGREE SIGN */
        KEY_PLUSMINUS = 0X00B1,  /* U+00B1 PLUS-MINUS SIGN */
        KEY_TWOSUPERIOR = 0X00B2,  /* U+00B2 SUPERSCRIPT TWO */
        KEY_THREESUPERIOR = 0X00B3,  /* U+00B3 SUPERSCRIPT THREE */
        KEY_ACUTE = 0X00B4,  /* U+00B4 ACUTE ACCENT */
        KEY_MU = 0X00B5,  /* U+00B5 MICRO SIGN */
        KEY_PARAGRAPH = 0X00B6,  /* U+00B6 PILCROW SIGN */
        KEY_PERIODCENTER = 0X00B7,  /* U+00B7 MIDDLE DOT */
        KEY_CEDILLA = 0X00B8,  /* U+00B8 CEDILLA */
        KEY_ONESUPERIOR = 0X00B9,  /* U+00B9 SUPERSCRIPT ONE */
        KEY_MASCULINE = 0X00BA,  /* U+00BA MASCULINE ORDINAL INDICATOR */
        KEY_GUILLEMOTRIGHT = 0X00BB,  /* U+00BB RIGHT-POINTING DOUBLE ANGLE QUOTATION MARK */
        KEY_ONEQUARTER = 0X00BC,  /* U+00BC VULGAR FRACTION ONE QUARTER */
        KEY_ONEHALF = 0X00BD,  /* U+00BD VULGAR FRACTION ONE HALF */
        KEY_THREEQUARTER = 0X00BE,  /* U+00BE VULGAR FRACTION THREE QUARTERS */
        KEY_QUESTIONDOWN = 0X00BF,  /* U+00BF INVERTED QUESTION MARK */

        KEY_BACKSPACE = 0XFF08,  /* BACK SPACE, BACK CHAR */
        KEY_TAB = 0XFF09,
        KEY_LINEFEED = 0XFF0A,  /* LINEFEED, LF */
        KEY_CLEAR = 0XFF0B,
        KEY_RETURN = 0XFF0D,  /* RETURN, ENTER */
        KEY_PAUSE = 0XFF13,  /* PAUSE, HOLD */
        KEY_SCROLL_LOCK = 0XFF14,
        KEY_SYS_REQ = 0XFF15,
        KEY_ESCAPE = 0XFF1B,
        KEY_DELETE = 0XFFFF,  /* DELETE, RUBOUT */

        KEY_HOME = 0XFF50,
        KEY_LEFT = 0XFF51,  /* MOVE LEFT, LEFT ARROW */
        KEY_UP = 0XFF52,  /* MOVE UP, UP ARROW */
        KEY_RIGHT = 0XFF53,  /* MOVE RIGHT, RIGHT ARROW */
        KEY_DOWN = 0XFF54,  /* MOVE DOWN, DOWN ARROW */
        KEY_PAGE_UP = 0XFF55,
        KEY_PAGE_DOWN = 0XFF56,
        KEY_END = 0XFF57,  /* EOL */
        KEY_BEGIN = 0XFF58,  /* BOL */

        KEY_NUMPAD0 = 0XFF9E,
        KEY_NUMPAD1 = 0XFF9C,
        KEY_NUMPAD2 = 0XFF99,
        KEY_NUMPAD3 = 0XFF9B,
        KEY_NUMPAD4 = 0XFF96,
        KEY_NUMPAD5 = 0XFF9D,
        KEY_NUMPAD6 = 0XFF98,
        KEY_NUMPAD7 = 0XFF95,
        KEY_NUMPAD8 = 0XFF97,
        KEY_NUMPAD9 = 0XFF9A,

        KEY_F1 = 0XFFBE,
        KEY_F2 = 0XFFBF,
        KEY_F3 = 0XFFC0,
        KEY_F4 = 0XFFC1,
        KEY_F5 = 0XFFC2,
        KEY_F6 = 0XFFC3,
        KEY_F7 = 0XFFC4,
        KEY_F8 = 0XFFC5,
        KEY_F9 = 0XFFC6,
        KEY_F10 = 0XFFC7,
        KEY_F11 = 0XFFC8,
        KEY_F12 = 0XFFC9,

        KEY_SHIFT_L = 0XFFE1,  /* LEFT SHIFT */
        KEY_SHIFT_R = 0XFFE2,  /* RIGHT SHIFT */
        KEY_CONTROL_L = 0XFFE3,  /* LEFT CONTROL */
        KEY_CONTROL_R = 0XFFE4,  /* RIGHT CONTROL */
        KEY_CAPS_LOCK = 0XFFE5,  /* CAPS LOCK */
        KEY_SHIFT_LOCK = 0XFFE6,  /* SHIFT LOCK */

        KEY_META_L = 0XFFE7,  /* LEFT META */
        KEY_META_R = 0XFFE8,  /* RIGHT META */
        KEY_ALT_L = 0XFFE9,  /* LEFT ALT */
        KEY_ALT_R = 0XFFEA,  /* RIGHT ALT */
        KEY_SUPER_L = 0XFFEB,  /* LEFT SUPER */
        KEY_SUPER_R = 0XFFEC,  /* RIGHT SUPER */
        KEY_HYPER_L = 0XFFED,  /* LEFT HYPER */
        KEY_HYPER_R = 0XFFEE,  /* RIGHT HYPER */
    }
}