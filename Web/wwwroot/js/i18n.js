const i18n = {
  en: {
    name: 'English',
    dir: 'ltr',
    q_1: 'Edit an existing question:',
    q_2: 'Or enter a question for:',
    q_3: `<div dir="ltr">Please write a template question that asks a speaker to produce the inflected form with the above morphosyntactic features:</div>`,
    q_4: `
<div dir="ltr">
<p>Make sure your question follows these rules:</p>
<ul>
<li>It must be written completely in English.</li>
<li>Use simple, clear language that <b>anyone can understand</b>.</li>
<li>Include all the required grammatical features.</li>
<li>Use "XXX" as a placeholder for the base form of the word (the lemma).</li>
</ul>
</div>`,
    q_5: `<div dir="ltr"><b>Need inspiration?</b> Below are example questions generated by AI systems. Use them as a guide to help you create your own.</div>`,
    prompt: `You are a field linguist working with native speakers to study the morphology of their language. The speakers are not trained in linguistics but understand English.
Ask a speaker how they would say the word "XXX" with these specific features: "FFF". 
Generate only the question for the speaker. "XXX" should be in the question. Do not use linguistic terms. Keep it under 50 words. Do not explain anything to me.`,
    q_submit: 'Submit',
    elicit_instructions:
      'Please answer the following question in your own language.',
    elicit_suggestions: 'Suggestions:',
    elicit_answer: 'Answer',
    elicit_skip: 'I am not sure, skip!',
    elicit_samples: 'The desired word but from other bases',
    elicit_from: 'from',
    elicit_end: 'We are out of items for now. Please come back later.',
    check_instructions: 'Is XXX the answer to the above questions?',
    check_yes: 'Yes',
    check_no: 'No',
  },

  es: {
    name: 'español',
    dir: 'ltr',
    q_1: 'Editar una pregunta existente:',
    q_2: 'O introducir una pregunta para:',
    q_3: `<div dir="ltr">Por favor, escribe una pregunta modelo que pida al hablante que produzca la forma flexiva con las características morfosintácticas mencionadas:</div>`,
    q_4: `
<div dir="ltr">
<p>Asegúrate de que tu pregunta siga estas reglas:</p>
<ul>
<li>Debe estar escrita completamente en español.</li>
<li>Usa un lenguaje sencillo y claro que <b>cualquiera pueda entender</b>.</li>
<li>Incluye todas las características gramaticales requeridas.</li>
<li>Usa "XXX" como marcador de posición para la forma base de la palabra (el lema).</li>
</ul>
</div>`,
    q_5: `<div dir="ltr"><b>¿Necesitas inspiración?</b> A continuación, se muestran ejemplos de preguntas generadas por sistemas de IA. Úsalos como guía para ayudarte a crear el tuyo propio.</div>`,
    prompt: `Eres un lingüista de campo que trabaja con hablantes nativos para estudiar la morfología de su lengua. Los hablantes no tienen formación lingüística, pero entienden español.
Pregúntale a un hablante cómo diría la palabra "XXX" con estas características específicas: "FFF".
Genera solo la pregunta para el hablante. "XXX" debe estar en la pregunta. No uses términos lingüísticos. Mantenla en menos de 50 palabras. No me expliques nada.`,
    q_submit: 'Enviar',
    elicit_instructions:
      'Por favor responda la siguiente pregunta en su propio idioma.',
    elicit_suggestions: 'Sugerencias:',
    elicit_answer: 'Respuesta',
    elicit_skip: 'No estoy seguro, saltar!',
    elicit_samples: 'La palabra deseada pero desde otras bases',
    elicit_from: 'de',
    elicit_end: 'Nos hemos quedado sin artículos por ahora. Vuelva más tarde.',
    check_instructions: '¿Es XXX la respuesta a las preguntas anteriores?',
    check_yes: 'Sí',
    check_no: 'No',
  },
  fr: {
    name: 'Français',
    dir: 'ltr',
    q_1: `Modifier une question existante :`,
    q_2: `Ou saisissez une question pour :`,
    q_3: `<div dir="ltr">Veuillez rédiger un modèle de question demandant au locuteur de produire la forme fléchie avec les caractéristiques morphosyntaxiques ci-dessus :</div>`,
    q_4: `<div dir="ltr">
<p>Assurez-vous que votre question respecte les règles suivantes :</p>
<ul>
<li>Elle doit être rédigée entièrement en français.</li>
<li>Utilisez un langage simple et clair, compréhensible par tous.</li>
<li>Incluez toutes les caractéristiques grammaticales requises.</li>
<li>Utilisez « XXX » comme espace réservé pour la forme de base du mot (le lemme).</li>
</ul>
</div>`,
    q_5: `<div dir="ltr"><b>Besoin d'inspiration ?</b> Vous trouverez ci-dessous des exemples de questions générées par des systèmes d'IA. Utilisez-les comme guide pour vous aider à créer le vôtre.</div>`,
    prompt: `Vous êtes un linguiste de terrain travaillant avec des locuteurs natifs pour étudier la morphologie de leur langue. Ces locuteurs ne sont pas formés en linguistique, mais comprennent le français.
Demandez à un locuteur comment il prononcerait le mot « XXX » avec cette caractéristique spécifique : « FFF ».
Générez uniquement la question pour le locuteur. « XXX » doit figurer dans la question. N'utilisez pas de termes linguistiques. Limitez-la à 50 mots. Ne m'expliquez rien.`,
    q_submit: 'Soumettre',
    elicit_instructions:
      'Veuillez répondre à la question suivante dans votre propre langue.',
    elicit_suggestions: 'Suggestions :',
    elicit_answer: 'Répondre',
    elicit_skip: 'Je ne suis pas sûr, passez !',
    elicit_samples: `Le mot souhaité, mais d'autres bases`,
    elicit_from: 'de',
    elicit_end: `Nous n'avons plus d'éléments pour le moment. Veuillez revenir plus tard.`,
    check_instructions: 'XXX est-il la réponse aux questions ci-dessus ?',
    check_yes: 'Oui',
    check_no: 'Non',
  },
  fa: {
    name: 'فارسی',
    dir: 'rtl',
    q_1: 'ویرایش یک سوال موجود:',
    q_2: 'یا یک سوال بنویسید برای:',
    q_3: `<div dir="rtl">لطفاً یک الگوی سوال بنویسید که از گوینده بخواهد واژه صرف شده را با ویژگی‌های صرفی-نحوی بالا تولید کند:</div>`,
    q_4: `<div dir="rtl">
<p>مطمئن شوید که سوال شما از این قوانین پیروی می‌کند:</p>
<ul>
<li>باید کاملاً به فارسی نوشته شود.</li>
<li>از زبانی ساده و واضح استفاده کنید که برای همه قابل فهم باشد.</li>
<li>تمام ویژگی‌های دستوری مورد نیاز را لحاظ کنید.</li>
<li>از "XXX" به عنوان جایگزین برای ریشه/مصدر استفاده کنید.</li>
</ul>
</div>`,
    q_5: `<div dir="rtl"><b>به الهام نیاز دارید؟</b> در زیر نمونه‌هایی از سوالات تولید شده توسط هوش مصنوعی آمده است. از آنها به عنوان راهنما برای ایجاد سیستم خودتان استفاده کنید.</div>`,
    prompt: `فرض کن تو یک زبانشناس میدانی هستی که می‌خواهی از گویشور یک زبانی سوال بپرسی تا ساختار صرف و نحو زبانش را یاد بگیری.
گویشور چیزی از زبانشناسی و اصطلاحات زبانشناسی نمی داند اما زبان فارسی را می‌فهمد.
از گویشور بپرس که چگونه فعل «XXX» را با این ویژگی‌های خاص صرف می‌کنند: «FFF».
فقط سوال را برای گویشور ایجاد کن. سوال نباید بیش از 50 کلمه باشد. از اصطلاحات تخصصی زبانشناسی استفاده نکن. 
حتما باید XXX در سوال باشد.`,
    q_submit: 'ثبت',
    elicit_instructions: 'لطفا به سوال زیر به زبان خودتان پاسخ دهید.',
    elicit_suggestions: 'پیشنهادها:',
    elicit_answer: 'پاسخ',
    elicit_skip: 'مطمئن نیستم، بعدی!',
    elicit_samples: 'کلمه مورد نظر اما از ریشه‌های دیگر',
    elicit_from: 'از ریشه‌ی',
    elicit_end: 'فعلا مورد دیگری وجود ندارد. لطفا بعدا بازگردید.',
    check_instructions: 'آیا XXX پاسخ سؤالات بالا است؟',
    check_yes: 'بله',
    check_no: 'نه',
  },

  ar: {
    name: 'عربي',
    dir: 'rtl',
    q_1: 'تعديل سؤال موجود:',
    q_2: 'أو اكتب سؤالاً لـ:',
    q_3: `<div dir="rtl">يرجى كتابة نموذج سؤال يطلب من المتحدث إنتاج الكلمة المتصرّفة بالخصائص الصرفية والنحوية المذكورة أعلاه:</div>`,
    q_4: `<div dir="rtl">
<p>تأكد من أن سؤالك يتبع القواعد التالية:</p>
<ul>
<li>يجب كتابته بالكامل باللغة الفارسية.</li>
<li>استخدم لغة بسيطة وواضحة ومفهومة للجميع.</li>
<li>أدرج جميع الخصائص النحوية المطلوبة.</li>
<li>استخدم "XXX" كبديل للجذر/المصدر.</li>
</ul>
</div>`,
    q_5: `<div dir="rtl"><div dir="rtl"><b>هل تحتاج إلى إلهام؟</b> فيما يلي أمثلة على أسئلة تم إنشاؤها بواسطة الذكاء الاصطناعي. استخدمها كدليل لإنشاء نظامك الخاص.</div>`,
    prompt: `لنفترض أنك عالم لغوي ميداني وتريد طرح أسئلة على متحدث أحادي اللغة لتعلم تصريفات اللغة وقواعدها.
المتحدث لا يعرف شيئا عن اللغويات والمصطلحات اللغوية ولكنه يفهم اللغة العربية.
اسأل المتحدث كيف يمكنه تصريف الفعل "XXX" مع هذه الميزات المحددة: "FFF".
فقط اطرح السؤال على المتحدث. لا ينبغي أن يتجاوز السؤال 50 كلمة. لا تستخدم المصطلحات اللغوية المتخصصة.
يجب أن يكون XXX في السؤال. `,
    q_submit: 'إرسال',
    elicit_instructions: 'الرجاء الإجابة على السؤال التالي بلغتك الخاصة.',
    elicit_suggestions: 'الاقتراحات:',
    elicit_answer: 'الإجابة',
    elicit_skip: 'لست متأكدًا، التالي!',
    elicit_samples: 'الكلمة المطلوبة ولكن من جذور أخرى',
    elicit_from: 'من جذر',
    elicit_end: 'لا يوجد شيء آخر في الوقت الراهن. الرجاء العودة لاحقا.',
    check_instructions: 'هل XXX هو الجواب على الأسئلة أعلاه؟',
    check_yes: 'نعم',
    check_no: 'لا',
  },
  tr: {
    name: 'Türkçe',
    dir: 'ltr',
    q_1: 'Mevcut bir soruyu düzenle:',
    q_2: 'Veya şu soru için bir soru gir:',
    q_3: `<div dir="ltr">Lütfen bir konuşmacıdan yukarıdaki morfo-syntactic özelliklere sahip çekimli formu üretmesini isteyen bir soru şablonu yazın:</div>`,
    q_4: `
<div dir="ltr">
<p>Sorunuzun şu kurallara uyduğundan emin olun:</p>
<ul>
<li>Tamamen Fransızca yazılmalıdır.</li>
<li>Herkesin anlayabileceği</b> basit ve açık bir dil kullanın.</li>
<li>Gereken tüm dilbilgisi özelliklerini ekleyin.</li>
<li>Kelimenin temel biçimi (lemma) için yer tutucu olarak "XXX" kullanın.</li>
</ul>
</div>`,
    q_5: `<div dir="ltr"><b>Gerekli ilham?</b> Aşağıda AI sistemleri tarafından oluşturulan örnek sorular bulunmaktadır. Bunları kendi sorularınızı oluşturmanıza yardımcı olacak bir kılavuz olarak kullanın.</div>`,
    prompt: `Siz, ana dili konuşanlarla dillerinin morfolojisini incelemek için çalışan bir saha dilbilimcisisiniz. Konuşanlar dilbilim konusunda eğitimli değillerdir ancak Türkçeyi anlarlar.
Konuşmacıya "XXX" kelimesini şu belirli özelliklerle nasıl söyleyeceğini sorun: "FFF".
Sadece konuşmacı için soruyu oluşturun. Soruda "XXX" bulunmalıdır. Dilbilimsel terimler kullanmayın. 50 kelimenin altında tutun. Bana hiçbir şey açıklamayın.`,
    q_submit: 'Gönder',
    elicit_instructions: 'Lütfen aşağıdaki soruyu kendi dilinizde cevaplayın.',
    elicit_suggestions: 'Öneriler:',
    elicit_answer: 'Cevap',
    elicit_skip: 'Emin değilim, Sonraki!',
    elicit_samples: 'İstenen kelime ama başka köklerden',
    elicit_from: 'şuradan',
    elicit_end: 'Şimdilik başka öğe yok. Lütfen daha sonra tekrar gelin.',
    check_instructions: 'XXX yukarıdaki soruların cevabı mı?',
    check_yes: 'Evet',
    check_no: 'Hayır',
  },
};
