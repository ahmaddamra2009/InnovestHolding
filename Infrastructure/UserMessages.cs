namespace InnovestHolding.Infrastructure;

/// <summary>
/// Every user-facing message added or moved by PR-A, in one place. Arabic only for now — declared
/// transitional debt (CTO decision 1): PR-B replaces this file with .resx (ar + en).
/// The constants are <c>const</c> so DataAnnotation attributes can reference them.
/// </summary>
public static class UserMessages
{
    // ── Generic validation ({0} = field display name) ──
    public const string Required = "حقل {0} مطلوب";
    public const string MaxLength = "حقل {0} يجب ألا يتجاوز {1} حرفاً";
    public const string LengthRange = "حقل {0} يجب أن يكون بين {2} و{1} حرفاً";
    public const string Range = "حقل {0} يجب أن يكون بين {1} و{2}";
    public const string Email = "البريد الإلكتروني غير صالح";
    public const string Phone = "رقم الهاتف غير صالح";
    public const string UnsafeHref = "الرابط غير صالح (المسموح: # أو مسار داخل الموقع أو http/https أو mailto/tel)";
    public const string InvalidValue = "القيمة المدخلة في حقل {0} غير صالحة";
    public const string ValueRequired = "حقل {0} مطلوب";
    // Model-binding messages that have no field name to show (no "{0}", so no double space — Stage 8 N3).
    public const string InvalidValueNoField = "القيمة المدخلة غير صالحة";
    public const string ValueRequiredNoField = "هذا الحقل مطلوب";
    public const string PasswordsDoNotMatch = "كلمة المرور الجديدة غير متطابقة";
    public const string CurrentPasswordWrong = "كلمة المرور الحالية غير صحيحة";

    // ── Public forms ──
    public const string RegistrationSuccess = "تم التسجيل بنجاح! سنتواصل معك قريباً";
    public const string ContactSuccess = "تم إرسال رسالتك بنجاح! سنرد عليك في أقرب وقت";
    public const string TooManyRequests = "عدد كبير من الطلبات، يرجى المحاولة لاحقاً";

    // ── Login ──
    public const string LoginInvalid = "اسم المستخدم أو كلمة المرور غير صحيحة";
    public const string LoginLocked = "محاولات كثيرة، حاول لاحقاً";

    // ── Error page (no developer text — Stage 4 L6) ──
    public const string ErrorTitle = "حدث خطأ";
    public const string ErrorGeneric = "تعذّر إكمال طلبك بسبب خطأ غير متوقع. يرجى المحاولة مرة أخرى لاحقاً.";
    public const string ErrorReference = "رقم المرجع";
    public const string BackToHome = "العودة إلى الصفحة الرئيسية";

    /// <summary>Field display names used in the validation messages above.</summary>
    public static class Fields
    {
        public const string FullName = "الاسم الكامل";
        public const string Email = "البريد الإلكتروني";
        public const string Phone = "رقم الهاتف";
        public const string Interest = "مجال الاهتمام";
        public const string Subject = "الموضوع";
        public const string Message = "الرسالة";
        public const string Username = "اسم المستخدم";
        public const string Password = "كلمة المرور";
        public const string CurrentPassword = "كلمة المرور الحالية";
        public const string NewPassword = "كلمة المرور الجديدة";
        public const string ConfirmPassword = "تأكيد كلمة المرور";
        public const string Title = "العنوان";
        public const string Description = "الوصف";
        public const string EventDay = "اليوم";
        public const string EventMonth = "الشهر";
        public const string Location = "المكان";
        public const string Time = "الوقت";
        public const string SortOrder = "الترتيب";
        public const string Question = "السؤال";
        public const string Answer = "الجواب";
        public const string Summary = "الملخص";
        public const string Category = "الفئة";
        public const string NewsDate = "التاريخ";
        public const string Name = "الاسم";
        public const string Href = "الرابط";
        public const string Label = "التسمية";
        public const string Value = "القيمة";
        public const string Suffix = "اللاحقة";
        public const string Content = "المحتوى";
    }
}
