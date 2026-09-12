import { describe, expect, it } from 'vitest';
import type { RegistrationFormCustomFieldOption } from '@/shared/api/registrationFormSettingsTypes';
import {
  findDuplicateRegistrationOptionValues,
  moveRegistrationOption,
  nextRegistrationOptionOrder,
  reorderRegistrationOptions,
} from './registrationFieldOptionHelpers';

const FOOT: RegistrationFormCustomFieldOption = { textUz: 'Piyoda', value: 'foot', order: 1 };
const BUS: RegistrationFormCustomFieldOption = { textUz: 'Avtobus', value: 'bus', order: 2 };

describe('nextRegistrationOptionOrder', () => {
  it("bo'sh ro'yxatda 1 qaytaradi, aks holda eng katta `order`+1", () => {
    expect(nextRegistrationOptionOrder([])).toBe(1);
    expect(nextRegistrationOptionOrder([FOOT, BUS])).toBe(3);
  });
});

describe('findDuplicateRegistrationOptionValues', () => {
  it('takroriy `value`larni topadi, bo\'sh qiymatni e\'tiborsiz qoldiradi', () => {
    expect(findDuplicateRegistrationOptionValues([FOOT, BUS]).size).toBe(0);
    expect(
      findDuplicateRegistrationOptionValues([FOOT, { ...BUS, value: 'foot' }]),
    ).toEqual(new Set(['foot']));
    expect(
      findDuplicateRegistrationOptionValues([
        { textUz: '', value: '', order: 1 },
        { textUz: '', value: '', order: 2 },
      ]).size,
    ).toBe(0);
  });
});

describe('reorderRegistrationOptions', () => {
  it('`order`ni 1..n ga qayta tartiblaydi', () => {
    const reordered = reorderRegistrationOptions([
      { ...FOOT, order: 5 },
      { ...BUS, order: 9 },
    ]);
    expect(reordered.map((o) => o.order)).toEqual([1, 2]);
  });
});

describe('moveRegistrationOption', () => {
  it('variantni bir pog\'ona ko\'chiradi va tartibni yangilaydi', () => {
    const next = moveRegistrationOption([FOOT, BUS], 1, -1);
    expect(next.map((o) => o.value)).toEqual(['bus', 'foot']);
    expect(next.map((o) => o.order)).toEqual([1, 2]);
  });

  it("chegaradan tashqariga chiqarishga urinilsa ro'yxat (nusxa) o'zgarishsiz qaytadi", () => {
    expect(moveRegistrationOption([FOOT, BUS], 0, -1)).toEqual([FOOT, BUS]);
    expect(moveRegistrationOption([FOOT, BUS], 1, 1)).toEqual([FOOT, BUS]);
  });
});
