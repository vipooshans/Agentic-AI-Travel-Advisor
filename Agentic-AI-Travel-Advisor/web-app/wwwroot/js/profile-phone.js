(() => {
    const nationality = document.getElementById("nationalitySelect");
    const storedPhone = document.getElementById("PhoneNumber");
    const phoneInput = document.getElementById("phoneNationalNumber");
    const dialCode = document.getElementById("phoneDialCode");
    const phoneError = document.getElementById("phoneError");
    const profileForm = phoneInput?.closest("form");

    if (!nationality || !storedPhone || !phoneInput || !dialCode || !phoneError || !profileForm) {
        return;
    }

    const dialingData = `
AD:376 AE:971 AF:93 AG:1 AI:1 AL:355 AM:374 AO:244 AQ:672 AR:54 AS:1 AT:43 AU:61 AW:297 AX:358 AZ:994
BA:387 BB:1 BD:880 BE:32 BF:226 BG:359 BH:973 BI:257 BJ:229 BL:590 BM:1 BN:673 BO:591 BQ:599 BR:55 BS:1 BT:975 BV:47 BW:267 BY:375 BZ:501
CA:1 CC:61 CD:243 CF:236 CG:242 CH:41 CI:225 CK:682 CL:56 CM:237 CN:86 CO:57 CR:506 CU:53 CV:238 CW:599 CX:61 CY:357 CZ:420
DE:49 DJ:253 DK:45 DM:1 DO:1 DZ:213
EC:593 EE:372 EG:20 EH:212 ER:291 ES:34 ET:251
FI:358 FJ:679 FK:500 FM:691 FO:298 FR:33
GA:241 GB:44 GD:1 GE:995 GF:594 GG:44 GH:233 GI:350 GL:299 GM:220 GN:224 GP:590 GQ:240 GR:30 GS:500 GT:502 GU:1 GW:245 GY:592
HK:852 HM:672 HN:504 HR:385 HT:509 HU:36
ID:62 IE:353 IL:972 IM:44 IN:91 IO:246 IQ:964 IR:98 IS:354 IT:39
JE:44 JM:1 JO:962 JP:81
KE:254 KG:996 KH:855 KI:686 KM:269 KN:1 KP:850 KR:82 KW:965 KY:1 KZ:7
LA:856 LB:961 LC:1 LI:423 LK:94 LR:231 LS:266 LT:370 LU:352 LV:371 LY:218
MA:212 MC:377 MD:373 ME:382 MF:590 MG:261 MH:692 MK:389 ML:223 MM:95 MN:976 MO:853 MP:1 MQ:596 MR:222 MS:1 MT:356 MU:230 MV:960 MW:265 MX:52 MY:60 MZ:258
NA:264 NC:687 NE:227 NF:672 NG:234 NI:505 NL:31 NO:47 NP:977 NR:674 NU:683 NZ:64
OM:968
PA:507 PE:51 PF:689 PG:675 PH:63 PK:92 PL:48 PM:508 PN:64 PR:1 PS:970 PT:351 PW:680 PY:595
QA:974
RE:262 RO:40 RS:381 RU:7 RW:250
SA:966 SB:677 SC:248 SD:249 SE:46 SG:65 SH:290 SI:386 SJ:47 SK:421 SL:232 SM:378 SN:221 SO:252 SR:597 SS:211 ST:239 SV:503 SX:1 SY:963 SZ:268
TF:262
TC:1 TD:235 TG:228 TH:66 TJ:992 TK:690 TL:670 TM:993 TN:216 TO:676 TR:90 TT:1 TV:688 TW:886 TZ:255
UA:380 UG:256 UM:1 US:1 UY:598 UZ:998
VA:39 VC:1 VE:58 VG:1 VI:1 VN:84 VU:678 WF:681 WS:685 XK:383 YE:967 YT:262 ZA:27 ZM:260 ZW:263
`.trim().split(/\s+/).map((entry) => {
        const [region, callingCode] = entry.split(":");
        return { region, callingCode: /^\d+$/.test(callingCode) ? callingCode : "" };
    }).filter((country) => country.callingCode);

    const displayNames = new Intl.DisplayNames(["en"], { type: "region" });
    const countries = dialingData.map((country) => ({
        ...country,
        name: country.region === "XK" ? "Kosovo" : displayNames.of(country.region)
    })).sort((a, b) => a.name.localeCompare(b.name));

    const countryByRegion = new Map(countries.map((country) => [country.region, country]));
    const legacyNationalityRegions = new Map([
        ["sri lankan", "LK"],
        ["british", "GB"],
        ["american", "US"],
        ["united states of america", "US"],
        ["indian", "IN"],
        ["canadian", "CA"],
        ["australian", "AU"],
        ["chinese", "CN"],
        ["japanese", "JP"],
        ["french", "FR"],
        ["german", "DE"]
    ]);

    for (const country of countries) {
        const option = document.createElement("option");
        option.value = country.name;
        option.textContent = `${country.name} (+${country.callingCode})`;
        option.dataset.region = country.region;
        nationality.append(option);
    }

    const originalNationality = (nationality.dataset.currentNationality || nationality.value).trim();
    const legacyRegion = legacyNationalityRegions.get(originalNationality.toLowerCase());
    let selectedCountry = countries.find((country) =>
        country.name.toLowerCase() === originalNationality.toLowerCase()
    ) ?? (legacyRegion ? countryByRegion.get(legacyRegion) : undefined);

    const savedPhoneDigits = storedPhone.value.replace(/\D/g, "");
    if (!selectedCountry && savedPhoneDigits) {
        selectedCountry = [...countries]
            .sort((a, b) => b.callingCode.length - a.callingCode.length)
            .find((country) => savedPhoneDigits.startsWith(country.callingCode));
    }

    if (selectedCountry) {
        nationality.value = selectedCountry.name;
        phoneInput.value = savedPhoneDigits.startsWith(selectedCountry.callingCode)
            ? savedPhoneDigits.slice(selectedCountry.callingCode.length)
            : savedPhoneDigits;
        if (selectedCountry.region === "LK" && phoneInput.value.length === 9) {
            phoneInput.value = `0${phoneInput.value}`;
        }
    } else {
        nationality.value = "";
        phoneInput.value = savedPhoneDigits;
    }

    const refreshPhonePrefix = () => {
        const country = countries.find((item) => item.name === nationality.value);
        dialCode.textContent = country ? `+${country.callingCode}` : "+";
        phoneInput.placeholder = country ? "Enter phone number" : "Select your country first";
        phoneInput.maxLength = country?.region === "LK" ? 10 : 15;
        if (country?.region === "LK" && phoneInput.value.length > 10) {
            phoneInput.value = phoneInput.value.slice(0, 10);
        }
        phoneError.textContent = "";
        updateStoredPhone();
    };

    const updateStoredPhone = () => {
        const country = countries.find((item) => item.name === nationality.value);
        const digits = phoneInput.value.replace(/\D/g, "");
        if (!country || !digits) {
            storedPhone.value = "";
            return;
        }

        const nationalDigits = digits.startsWith("0") ? digits.slice(1) : digits;
        storedPhone.value = `+${country.callingCode}${nationalDigits}`;
    };

    nationality.addEventListener("change", refreshPhonePrefix);
    phoneInput.addEventListener("input", () => {
        const digits = phoneInput.value.replace(/\D/g, "");
        if (phoneInput.value !== digits) {
            phoneInput.value = digits;
        }
        phoneError.textContent = "";
        updateStoredPhone();
    });

    profileForm.addEventListener("submit", (event) => {
        const country = countries.find((item) => item.name === nationality.value);
        const digits = phoneInput.value;
        phoneError.textContent = "";

        if (!digits) {
            storedPhone.value = "";
            return;
        }

        if (!country) {
            event.preventDefault();
            phoneError.textContent = "Select your country to include its calling code.";
            nationality.focus();
            return;
        }

        const validLength = country.region === "LK"
            ? digits.length === 10 && digits.startsWith("0")
            : digits.length >= 4 && digits.length <= 15;
        if (!validLength) {
            event.preventDefault();
            phoneError.textContent = country.region === "LK"
                ? "A Sri Lankan phone number must contain 10 digits and start with 0."
                : "Enter a phone number containing 4 to 15 digits.";
            phoneInput.focus();
            return;
        }

        updateStoredPhone();
    });

    refreshPhonePrefix();
})();
