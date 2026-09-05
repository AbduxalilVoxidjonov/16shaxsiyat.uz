/**
 * Route xaritasi — docs/10-frontend-arxitektura.md, 3-bo'lim.
 * Barcha havolalar shu obyekt orqali quriladi (hardcode path yo'q).
 */
export const ROUTES = {
  /**
   * Ommaviy tanishtiruv (marketing) sahifalari — maktab rahbarlari, psixologlar va ota-onalar
   * uchun. Test oqimidan (`public`) ATAYLAB ajratilgan: test faqat maktab havolasi orqali
   * (`/t/:slug`) ochiladi, bu yerdagi sahifalar esa sessiyaga umuman bog'liq emas.
   */
  marketing: {
    home: '/',
    methodology: '/metodika',
    /**
     * Bitta shaxsiyat tipining sahifasi (`/metodika/intj`) — `/metodika` dagi tiplar
     * bo'limidan ochiladi. URL da kod kichik harfda (havolalar bir xil ko'rinishi uchun),
     * sahifa esa katta harfga keltirib qidiradi.
     */
    type: (code: string) => `/metodika/${code.toLowerCase()}`,
    about: '/biz-haqimizda',
    contact: '/aloqa',
  },
  /**
   * Ommaviy foydalanuvchi kabineti (P47) — Telegram orqali kirgan tashqi foydalanuvchi
   * uchun. Maktab oqimidan (`public`) MUSTAQIL: u yerda kirish ham, ro'yxatdan o'tish ham
   * talab qilinmaydi, bu yerda esa hamma narsa JWT egaligiga tayanadi (`docs/07` §5).
   */
  account: {
    login: '/kirish',
    home: '/kabinet',
    /** Maktabsiz test uchun anketa — `POST /api/me/sessions` (`docs/07` §5.4). */
    startTest: '/kabinet/test',
    /** Kabinetdagi arxiv natija — `GET /api/me/assessments/{id}/result` (`docs/07` §5.3). */
    result: (assessmentId: string) => `/kabinet/natijalar/${assessmentId}`,
  },
  public: {
    landing: (slug: string) => `/t/${slug}`,
    register: (slug: string) => `/t/${slug}/register`,
    test: (slug: string, testCode: string) => `/t/${slug}/test/${testCode}`,
    testDone: (slug: string, testCode: string) => `/t/${slug}/test/${testCode}/done`,
    finish: (slug: string) => `/t/${slug}/finish`,
    result: (slug: string) => `/t/${slug}/result`,
  },
  admin: {
    login: '/admin/login',
    dashboard: '/admin',
    schools: '/admin/schools',
    schoolDetail: (id: string) => `/admin/schools/${id}`,
    students: '/admin/students',
    studentProfile: (id: string) => `/admin/students/${id}`,
    assessments: '/admin/assessments',
    assessmentDetail: (id: string) => `/admin/assessments/${id}`,
    catalog: '/admin/catalog',
    catalogTestDetail: (id: string) => `/admin/catalog/tests/${id}`,
    programs: '/admin/programs',
    programDetail: (id: string) => `/admin/programs/${id}`,
    ai: '/admin/ai',
    audit: '/admin/audit',
    settings: '/admin/settings',
  },
} as const;

/** `react-router` uchun yo'l naqshlari (parametr o'rniga `:param`). */
export const ROUTE_PATTERNS = {
  marketing: {
    home: '/',
    methodology: '/metodika',
    type: '/metodika/:kod',
    about: '/biz-haqimizda',
    contact: '/aloqa',
  },
  account: {
    login: '/kirish',
    home: '/kabinet',
    startTest: '/kabinet/test',
    result: '/kabinet/natijalar/:assessmentId',
  },
  public: {
    landing: '/t/:slug',
    register: '/t/:slug/register',
    test: '/t/:slug/test/:testCode',
    testDone: '/t/:slug/test/:testCode/done',
    finish: '/t/:slug/finish',
    result: '/t/:slug/result',
  },
  admin: {
    login: '/admin/login',
    dashboard: '/admin',
    schools: '/admin/schools',
    schoolDetail: '/admin/schools/:id',
    students: '/admin/students',
    studentProfile: '/admin/students/:id',
    assessments: '/admin/assessments',
    assessmentDetail: '/admin/assessments/:id',
    catalog: '/admin/catalog',
    catalogTestDetail: '/admin/catalog/tests/:id',
    programs: '/admin/programs',
    programDetail: '/admin/programs/:id',
    ai: '/admin/ai',
    audit: '/admin/audit',
    settings: '/admin/settings',
  },
} as const;
