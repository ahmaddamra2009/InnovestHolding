namespace InnovestHolding.Infrastructure.Localization;

/// <summary>
/// Resource key names of Resources/SharedResource(.en).resx — the ONLY place messages are named. Values live in the
/// resx files (neutral = Arabic, which is also the fallback for a missing English key). <c>const</c> so DataAnnotation
/// attributes can use them; the parity test reflects over every constant here.
/// </summary>
public static class MessageKeys
{
    public const string Required = "Msg_Required";
    public const string MaxLength = "Msg_MaxLength";
    public const string LengthRange = "Msg_LengthRange";
    public const string Range = "Msg_Range";
    public const string Email = "Msg_Email";
    public const string Phone = "Msg_Phone";
    public const string UnsafeHref = "Msg_UnsafeHref";
    public const string HttpsUrl = "Msg_HttpsUrl";
    public const string BaseUrl = "Msg_BaseUrl";
    public const string SitePath = "Msg_SitePath";
    public const string Highlight = "Msg_Highlight";
    public const string InvalidValue = "Msg_InvalidValue";
    public const string ValueRequired = "Msg_ValueRequired";
    public const string InvalidValueNoField = "Msg_InvalidValueNoField";
    public const string ValueRequiredNoField = "Msg_ValueRequiredNoField";
    public const string PasswordsDoNotMatch = "Msg_PasswordsDoNotMatch";
    public const string CurrentPasswordWrong = "Msg_CurrentPasswordWrong";
    public const string InterestInvalid = "Msg_InterestInvalid";
    public const string RegistrationSuccess = "Msg_RegistrationSuccess";
    public const string ContactSuccess = "Msg_ContactSuccess";
    public const string ForumRegistrationSuccess = "Msg_ForumRegistrationSuccess";
    public const string JoinSuccess = "Msg_JoinSuccess";
    public const string SponsorshipSuccess = "Msg_SponsorshipSuccess";
    public const string InterestSuccess = "Msg_InterestSuccess";
    public const string ConsentRequired = "Msg_ConsentRequired";
    public const string OptionInvalid = "Msg_OptionInvalid";
    public const string TooManyRequests = "Msg_TooManyRequests";
    /// <summary>A lead form's global cap is full; {0} = the contact email (SiteContents contact_email).</summary>
    public const string LeadFormFull = "Msg_LeadFormFull";
    public const string LoginInvalid = "Msg_LoginInvalid";
    public const string LoginLocked = "Msg_LoginLocked";
    public const string ErrorTitle = "Msg_ErrorTitle";
    public const string ErrorGeneric = "Msg_ErrorGeneric";
    public const string ErrorReference = "Msg_ErrorReference";
    public const string BackToHome = "Msg_BackToHome";
    public const string LogoLimits = "Msg_LogoLimits";
    public const string EndBeforeStart = "Msg_EndBeforeStart";
    public const string DateRequired = "Msg_DateRequired";
    public const string UtcOffset = "Msg_UtcOffset";
    /// <summary>Content editor: contact_phone / org_address_country (C5 final, SEO-C5 §7).</summary>
    public const string ContentPhone = "Msg_ContentPhone";
    public const string ContentCountryCode = "Msg_ContentCountryCode";
    public const string SlugTaken = "Msg_SlugTaken";
    public const string SlugReserved = "Msg_SlugReserved";
    public const string SlugInvalid = "Msg_SlugInvalid";
    public const string ImageStorage = "Msg_ImageStorage";
    public const string ImageSize = "Msg_ImageSize";
    public const string ImageAnimated = "Msg_ImageAnimated";
    public const string ImageDimensions = "Msg_ImageDimensions";
    public const string ImageCorrupt = "Msg_ImageCorrupt";
    public const string ImageMismatch = "Msg_ImageMismatch";
    public const string ImageType = "Msg_ImageType";

    public static class Fields
    {
        public const string FullName = "Field_FullName";
        public const string Email = "Field_Email";
        public const string Phone = "Field_Phone";
        public const string Interest = "Field_Interest";
        public const string Subject = "Field_Subject";
        public const string Message = "Field_Message";
        public const string Consent = "Field_Consent";
        public const string Country = "Field_Country";
        public const string Event = "Field_Event";
        public const string ApplicantType = "Field_ApplicantType";
        public const string StartupName = "Field_StartupName";
        public const string InvestorType = "Field_InvestorType";
        public const string TicketSize = "Field_TicketSize";
        public const string Sectors = "Field_Sectors";
        public const string Sector = "Field_Sector";
        public const string FundingSought = "Field_FundingSought";
        public const string WebsiteUrl = "Field_WebsiteUrl";
        public const string Pitch = "Field_Pitch";
        public const string CompanyName = "Field_CompanyName";
        public const string Unit = "Field_Unit";
        public const string Package = "Field_Package";
        public const string Opportunity = "Field_Opportunity";
        public const string AdminNote = "Field_AdminNote";
        public const string Username = "Field_Username";
        public const string Password = "Field_Password";
        public const string CurrentPassword = "Field_CurrentPassword";
        public const string NewPassword = "Field_NewPassword";
        public const string ConfirmPassword = "Field_ConfirmPassword";
        public const string EventDay = "Field_EventDay";
        public const string SortOrder = "Field_SortOrder";
        public const string Href = "Field_Href";
        public const string Value = "Field_Value";
        public const string Suffix = "Field_Suffix";
        public const string LinkUrl = "Field_LinkUrl";
        public const string Url = "Field_Url";
        public const string Title = "Field_Title";
        public const string TitleEn = "Field_TitleEn";
        public const string Description = "Field_Description";
        public const string DescriptionEn = "Field_DescriptionEn";
        public const string EventMonth = "Field_EventMonth";
        public const string EventMonthEn = "Field_EventMonthEn";
        public const string Location = "Field_Location";
        public const string LocationEn = "Field_LocationEn";
        public const string Time = "Field_Time";
        public const string TimeEn = "Field_TimeEn";
        public const string Question = "Field_Question";
        public const string QuestionEn = "Field_QuestionEn";
        public const string Answer = "Field_Answer";
        public const string AnswerEn = "Field_AnswerEn";
        public const string Summary = "Field_Summary";
        public const string SummaryEn = "Field_SummaryEn";
        public const string Category = "Field_Category";
        public const string CategoryEn = "Field_CategoryEn";
        public const string NewsDate = "Field_NewsDate";
        public const string NewsDateEn = "Field_NewsDateEn";
        public const string Name = "Field_Name";
        public const string NameEn = "Field_NameEn";
        public const string Label = "Field_Label";
        public const string LabelEn = "Field_LabelEn";
        public const string ShortLabel = "Field_ShortLabel";
        public const string ShortLabelEn = "Field_ShortLabelEn";
        public const string Text = "Field_Text";
        public const string TextEn = "Field_TextEn";
        public const string Intro = "Field_Intro";
        public const string IntroEn = "Field_IntroEn";
        public const string Body = "Field_Body";
        public const string BodyEn = "Field_BodyEn";
        public const string EmptyText = "Field_EmptyText";
        public const string EmptyTextEn = "Field_EmptyTextEn";
        public const string MetaTitle = "Field_MetaTitle";
        public const string MetaTitleEn = "Field_MetaTitleEn";
        public const string MetaDescription = "Field_MetaDescription";
        public const string MetaDescriptionEn = "Field_MetaDescriptionEn";
        public const string Slug = "Field_Slug";
        public const string City = "Field_City";
        public const string CityEn = "Field_CityEn";
        public const string Address = "Field_Address";
        public const string AddressEn = "Field_AddressEn";
        public const string OnlineUrl = "Field_OnlineUrl";
        public const string StartsAt = "Field_StartsAt";
        public const string EndsAt = "Field_EndsAt";
        public const string UtcOffset = "Field_UtcOffset";
        public const string ImageAlt = "Field_ImageAlt";
        public const string ImageAltEn = "Field_ImageAltEn";
        public const string JobTitle = "Field_JobTitle";
        public const string JobTitleEn = "Field_JobTitleEn";
        public const string Organization = "Field_Organization";
        public const string OrganizationEn = "Field_OrganizationEn";
        public const string LinkedInUrl = "Field_LinkedInUrl";
        public const string XUrl = "Field_XUrl";
        public const string Season = "Field_Season";
        public const string EpisodeNumber = "Field_EpisodeNumber";
        public const string AiredOn = "Field_AiredOn";
        public const string VideoUrl = "Field_VideoUrl";
        public const string Stage = "Field_Stage";
        public const string StageEn = "Field_StageEn";
        public const string Ask = "Field_Ask";
        public const string AskEn = "Field_AskEn";
        public const string Price = "Field_Price";
        public const string PriceEn = "Field_PriceEn";
        public const string Benefits = "Field_Benefits";
        public const string BenefitsEn = "Field_BenefitsEn";
    }

    public static class Admin
    {
        public const string ContentSaved = "Admin_ContentSaved";
        public const string EventAdded = "Admin_EventAdded";
        public const string EventUpdated = "Admin_EventUpdated";
        public const string EventDeleted = "Admin_EventDeleted";
        public const string FaqAdded = "Admin_FaqAdded";
        public const string FaqUpdated = "Admin_FaqUpdated";
        public const string FaqDeleted = "Admin_FaqDeleted";
        public const string NewsAdded = "Admin_NewsAdded";
        public const string NewsUpdated = "Admin_NewsUpdated";
        public const string NewsDeleted = "Admin_NewsDeleted";
        public const string NavAdded = "Admin_NavAdded";
        public const string NavUpdated = "Admin_NavUpdated";
        public const string NavDeleted = "Admin_NavDeleted";
        public const string PartnerAdded = "Admin_PartnerAdded";
        public const string PartnerUpdated = "Admin_PartnerUpdated";
        public const string PartnerDeleted = "Admin_PartnerDeleted";
        public const string PlatformUpdated = "Admin_PlatformUpdated";
        public const string StatUpdated = "Admin_StatUpdated";
        public const string PasswordChanged = "Admin_PasswordChanged";
        public const string MessageDeleted = "Admin_MessageDeleted";
        public const string RegistrationDeleted = "Admin_RegistrationDeleted";
        public const string LeadDeleted = "Admin_LeadDeleted";
        public const string LeadUpdated = "Admin_LeadUpdated";
        public const string LeadStatusNew = "Admin_LeadStatusNew";
        public const string LeadStatusContacted = "Admin_LeadStatusContacted";
        public const string LeadStatusClosed = "Admin_LeadStatusClosed";
        // The lead CSV export: column headers and cell words (current culture; the admin is Arabic today).
        public const string CsvType = "Admin_CsvType";
        public const string CsvId = "Admin_CsvId";
        public const string CsvDate = "Admin_CsvDate";
        public const string CsvName = "Admin_CsvName";
        public const string CsvEmail = "Admin_CsvEmail";
        public const string CsvPhone = "Admin_CsvPhone";
        public const string CsvDetails = "Admin_CsvDetails";
        public const string CsvStatus = "Admin_CsvStatus";
        public const string CsvRead = "Admin_CsvRead";
        public const string CsvCulture = "Admin_CsvCulture";
        public const string CsvNote = "Admin_CsvNote";
        public const string CsvYes = "Admin_CsvYes";
        public const string CsvNo = "Admin_CsvNo";
        public const string CsvArabic = "Admin_CsvArabic";
        public const string CsvEnglish = "Admin_CsvEnglish";
        public const string CsvReference = "Admin_CsvReference";
        public const string CsvReferenceId = "Admin_CsvReferenceId";
        public const string OptionAdded = "Admin_OptionAdded";
        public const string OptionUpdated = "Admin_OptionUpdated";
        public const string OptionDeleted = "Admin_OptionDeleted";
        public const string BenefitAdded = "Admin_BenefitAdded";
        public const string BenefitUpdated = "Admin_BenefitUpdated";
        public const string BenefitDeleted = "Admin_BenefitDeleted";
        public const string SocialUpdated = "Admin_SocialUpdated";
        public const string PageSaved = "Admin_PageSaved";
        public const string PageMenuToggled = "Admin_PageMenuToggled";
        public const string OrderChanged = "Admin_OrderChanged";
        public const string ItemAdded = "Admin_ItemAdded";
        public const string ItemUpdated = "Admin_ItemUpdated";
        public const string ItemDeleted = "Admin_ItemDeleted";
        public const string StatusToggled = "Admin_StatusToggled";
        public const string EnHint = "Admin_EnHint";
        public const string NeutralHint = "Admin_NeutralHint";
        public const string ListHint = "Admin_ListHint";
        public const string GroupAdded = "Admin_GroupAdded";
        public const string GroupUpdated = "Admin_GroupUpdated";
        public const string GroupDeleted = "Admin_GroupDeleted";
        public const string PhotoSaved = "Admin_PhotoSaved";
        public const string ImageHintGeneral = "Admin_ImageHintGeneral";
        public const string ImageHintLogo = "Admin_ImageHintLogo";
        public const string SectorInUse = "Admin_SectorInUse";
        // Admin › رسائل وأزرار الموقع (Infrastructure/Content/PublicTexts).
        public const string SiteTextsSaved = "Admin_SiteTextsSaved";
        public const string SiteTextReset = "Admin_SiteTextReset";
        public const string SiteTextGroupReset = "Admin_SiteTextGroupReset";
        /// <summary>{0} = the text, {1} = the language, {2} = the placeholders the default has.</summary>
        public const string SiteTextPlaceholders = "Admin_SiteTextPlaceholders";
        /// <summary>{0} = the text, {1} = the language.</summary>
        public const string SiteTextMarkup = "Admin_SiteTextMarkup";
        /// <summary>{0} = the text, {1} = the language.</summary>
        public const string SiteTextFormatSpec = "Admin_SiteTextFormatSpec";

        public const string BrandSaved = "Admin_BrandSaved";
        public const string BrandReverted = "Admin_BrandReverted";
        public const string ThemeColorInvalid = "Admin_ThemeColorInvalid";
        public const string ThemeColorSaved = "Admin_ThemeColorSaved";
        /// <summary>The login page's site name when the content cannot be read (C4's hard-coded value).</summary>
        public const string LoginSiteName = "Admin_LoginSiteName";
    }

    public static class Ui
    {
        public const string LangSwitchText = "Ui_LangSwitchText";
        public const string LangSwitchAria = "Ui_LangSwitchAria";
        public const string LangDrawerText = "Ui_LangDrawerText";
        public const string NavAria = "Ui_NavAria";
        public const string MenuAria = "Ui_MenuAria";
        public const string FormFullName = "Ui_FormFullName";
        public const string FormFullNamePlaceholderRegister = "Ui_FormFullNamePlaceholderRegister";
        public const string FormFullNamePlaceholderContact = "Ui_FormFullNamePlaceholderContact";
        public const string FormEmail = "Ui_FormEmail";
        public const string FormEmailPlaceholder = "Ui_FormEmailPlaceholder";
        public const string FormPhone = "Ui_FormPhone";
        public const string FormPhonePlaceholder = "Ui_FormPhonePlaceholder";
        public const string FormInterest = "Ui_FormInterest";
        public const string FormInterestPlaceholder = "Ui_FormInterestPlaceholder";
        public const string FormSubject = "Ui_FormSubject";
        public const string FormSubjectPlaceholder = "Ui_FormSubjectPlaceholder";
        public const string FormMessage = "Ui_FormMessage";
        public const string FormMessagePlaceholder = "Ui_FormMessagePlaceholder";
        public const string FormRegistering = "Ui_FormRegistering";
        public const string FormSending = "Ui_FormSending";
        public const string FormGenericError = "Ui_FormGenericError";
        public const string ContactSubmit = "Ui_ContactSubmit";
        public const string FormRequiredNote = "Ui_FormRequiredNote";
        public const string FormChoose = "Ui_FormChoose";
        public const string FormOrganization = "Ui_FormOrganization";
        public const string FormJobTitle = "Ui_FormJobTitle";
        public const string FormCountry = "Ui_FormCountry";
        public const string FormEvent = "Ui_FormEvent";
        public const string FormEventAny = "Ui_FormEventAny";
        public const string FormKindInvestor = "Ui_FormKindInvestor";
        public const string FormKindStartup = "Ui_FormKindStartup";
        public const string FormInvestorDetails = "Ui_FormInvestorDetails";
        public const string FormStartupDetails = "Ui_FormStartupDetails";
        public const string FormInvestorType = "Ui_FormInvestorType";
        public const string FormTicketSize = "Ui_FormTicketSize";
        public const string FormSectors = "Ui_FormSectors";
        public const string FormStartupName = "Ui_FormStartupName";
        public const string FormSector = "Ui_FormSector";
        public const string FormStage = "Ui_FormStage";
        public const string FormFunding = "Ui_FormFunding";
        public const string FormWebsite = "Ui_FormWebsite";
        public const string FormLinkedIn = "Ui_FormLinkedIn";
        public const string FormPitch = "Ui_FormPitch";
        public const string FormCompanyName = "Ui_FormCompanyName";
        public const string FormContactName = "Ui_FormContactName";
        public const string FormUnit = "Ui_FormUnit";
        public const string FormPackage = "Ui_FormPackage";
        public const string FormPackageAny = "Ui_FormPackageAny";
        public const string FormOpportunityAny = "Ui_FormOpportunityAny";
        public const string FormSubmitApplication = "Ui_FormSubmitApplication";
        public const string FormSubmitEnquiry = "Ui_FormSubmitEnquiry";
        public const string FormSubmitInterest = "Ui_FormSubmitInterest";
        public const string ContactAddress = "Ui_ContactAddress";
        public const string ContactPhone = "Ui_ContactPhone";
        public const string ContactEmail = "Ui_ContactEmail";
        public const string ContactHours = "Ui_ContactHours";
        public const string BreadcrumbAria = "Ui_BreadcrumbAria";
        public const string SubmenuToggle = "Ui_SubmenuToggle";
        public const string Explore = "Ui_Explore";
        public const string ViewAll = "Ui_ViewAll";
        public const string PagerAria = "Ui_PagerAria";
        public const string PreviousPage = "Ui_PreviousPage";
        public const string NextPage = "Ui_NextPage";
        public const string PageOf = "Ui_PageOf";
        public const string NotFoundTitle = "Ui_NotFoundTitle";
        public const string NotFoundText = "Ui_NotFoundText";
        public const string ContactWhatsApp = "Ui_ContactWhatsApp";
        public const string ContactWebsite = "Ui_ContactWebsite";
        public const string ContactSocial = "Ui_ContactSocial";
        public const string SkipToContent = "Ui_SkipToContent";
        public const string BackToTop = "Ui_BackToTop";
        public const string EventDate = "Ui_EventDate";
        public const string EventTime = "Ui_EventTime";
        public const string EventVenue = "Ui_EventVenue";
        public const string EventOnline = "Ui_EventOnline";
        public const string EventJoinOnline = "Ui_EventJoinOnline";
        public const string EventCancelled = "Ui_EventCancelled";
        public const string EventPostponed = "Ui_EventPostponed";
        public const string EventPast = "Ui_EventPast";
        public const string EventEndedNotice = "Ui_EventEndedNotice";
        public const string PublishedOn = "Ui_PublishedOn";
        public const string NewsSource = "Ui_NewsSource";
        public const string ArabicOnlyNotice = "Ui_ArabicOnlyNotice";
        public const string ViewDetails = "Ui_ViewDetails";
        public const string PlayVideo = "Ui_PlayVideo";
        public const string VideoNotice = "Ui_VideoNotice";
        public const string WatchVideo = "Ui_WatchVideo";
        public const string SeasonEpisode = "Ui_SeasonEpisode";
        public const string EpisodeNumber = "Ui_EpisodeNumber";
        public const string SeasonNumber = "Ui_SeasonNumber";
        public const string AiredOn = "Ui_AiredOn";
        public const string JobTitle = "Ui_JobTitle";
        public const string Organization = "Ui_Organization";
        public const string Sector = "Ui_Sector";
        public const string Stage = "Ui_Stage";
        public const string Ask = "Ui_Ask";
        public const string OpportunityOpen = "Ui_OpportunityOpen";
        public const string OpportunityClosed = "Ui_OpportunityClosed";
        public const string ExpressInterest = "Ui_ExpressInterest";
        public const string SponsorshipPackages = "Ui_SponsorshipPackages";
        public const string ProfilesAria = "Ui_ProfilesAria";
        public const string WatchOnYouTube = "Ui_WatchOnYouTube";
        public const string ExternalLink = "Ui_ExternalLink";
        /// <summary>Visually hidden after the privacy link of a consent box (it opens in a new tab).</summary>
        public const string OpensInNewTab = "Ui_OpensInNewTab";
    }

    /// <summary>C5 list screens (Nav/footer, services, social links): admin messages, Arabic admin.</summary>
    public static class AdminLists
    {
        public const string FooterLimit = "Admin_FooterLimit";
        public const string SocialExists = "Admin_SocialExists";
        public const string IconInvalid = "Admin_IconInvalid";
        public const string CustomSocialIncomplete = "Admin_CustomSocialIncomplete";
    }

    /// <summary>C5 Admin/PageSections (show/hide, order, counts): admin messages.</summary>
    public static class AdminSections
    {
        public const string LastVisible = "Admin_SectionLastVisible";
        /// <summary>{0} = the section.</summary>
        public const string Shown = "Admin_SectionShown";
        /// <summary>{0} = the section.</summary>
        public const string Hidden = "Admin_SectionHidden";
        /// <summary>{0} = the section, {1} = its new position, {2} = the number of movable sections.</summary>
        public const string Moved = "Admin_SectionMoved";
        /// <summary>{0} = the count's label, {1} = its maximum.</summary>
        public const string LimitInvalid = "Admin_SectionLimitInvalid";
    }
}
