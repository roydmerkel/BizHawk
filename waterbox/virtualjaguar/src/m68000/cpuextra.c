//
// Stuff that's neither inline or generated by gencpu.c, but depended upon by
// cpuemu.c.
//
// Originally part of UAE by Bernd Schmidt
// and released under the GPL v2 or later
//

#include "cpuextra.h"
#include "cpudefs.h"
#include "inlines.h"

uint16_t last_op_for_exception_3;
uint32_t last_addr_for_exception_3;
uint32_t last_fault_for_exception_3;

int OpcodeFamily;
int BusCyclePenalty = 0;
int CurrentInstrCycles;

struct regstruct regs;

//
// Make displacement effective address for 68000
//
uint32_t get_disp_ea_000(uint32_t base, uint32_t dp)
{
	int reg = (dp >> 12) & 0x0F;
	int32_t regd = regs.regs[reg];

	if ((dp & 0x800) == 0)
		regd = (int32_t)(int16_t)regd;

	return base + (int8_t)dp + regd;
}

//
// Create the Status Register from the flags
//
void MakeSR(void)
{
	regs.sr = ((regs.s << 13) | (regs.intmask << 8) | (GET_XFLG << 4)
		| (GET_NFLG << 3) | (GET_ZFLG << 2) | (GET_VFLG << 1) | GET_CFLG);
}

//
// Set up the flags from Status Register
//
void MakeFromSR(void)
{
	int olds = regs.s;

	regs.s = (regs.sr >> 13) & 1;
	regs.intmask = (regs.sr >> 8) & 7;
	SET_XFLG((regs.sr >> 4) & 1);
	SET_NFLG((regs.sr >> 3) & 1);
	SET_ZFLG((regs.sr >> 2) & 1);
	SET_VFLG((regs.sr >> 1) & 1);
	SET_CFLG(regs.sr & 1);

	if (olds != regs.s)
	{
		if (olds)
		{
			regs.isp = m68k_areg(regs, 7);
			m68k_areg(regs, 7) = regs.usp;
		}
		else
		{
			regs.usp = m68k_areg(regs, 7);
			m68k_areg(regs, 7) = regs.isp;
		}
	}
}

//
// Handle exceptions. We need a special case to handle MFP exceptions
// on Atari ST, because it's possible to change the MFP's vector base
// and get a conflict with 'normal' cpu exceptions.
//
void Exception(int nr, uint32_t oldpc, int ExceptionSource)
{
	uint32_t currpc = m68k_getpc(), newpc;

	MakeSR();

	if (!regs.s)
	{
		regs.usp = m68k_areg(regs, 7);
		m68k_areg(regs, 7) = regs.isp;
		regs.s = 1;
	}

	m68k_areg(regs, 7) -= 4;
	m68k_write_memory_32(m68k_areg(regs, 7), currpc);
	m68k_areg(regs, 7) -= 2;
	m68k_write_memory_16(m68k_areg(regs, 7), regs.sr);

	m68k_setpc(m68k_read_memory_32(4 * nr));
	fill_prefetch_0();
}

//
// DIVU
// Unsigned division
//
STATIC_INLINE int getDivu68kCycles_2 (uint32_t dividend, uint16_t divisor)
{
	int mcycles;
	uint32_t hdivisor;
	int i;

	if (divisor == 0)
		return 0;

	if ((dividend >> 16) >= divisor)
		return (mcycles = 5) * 2;

	mcycles = 38;
	hdivisor = divisor << 16;

	for(i=0; i<15; i++)
	{
		uint32_t temp;
		temp = dividend;

		dividend <<= 1;

		if ((int32_t)temp < 0)
			dividend -= hdivisor;
		else
		{
			mcycles += 2;

			if (dividend >= hdivisor)
			{
				dividend -= hdivisor;
				mcycles--;
			}
		}
	}

	return mcycles * 2;
}

int getDivu68kCycles(uint32_t dividend, uint16_t divisor)
{
	int v = getDivu68kCycles_2(dividend, divisor) - 4;
	return v;
}

//
// DIVS
// Signed division
//
STATIC_INLINE int getDivs68kCycles_2(int32_t dividend, int16_t divisor)
{
	int mcycles;
	uint32_t aquot;
	int i;

	if (divisor == 0)
		return 0;

	mcycles = 6;

	if (dividend < 0)
		mcycles++;

	if (((uint32_t)abs(dividend) >> 16) >= (uint16_t)abs(divisor))
		return (mcycles + 2) * 2;

	aquot = (uint32_t)abs(dividend) / (uint16_t)abs(divisor);

	mcycles += 55;

	if (divisor >= 0)
	{
		if (dividend >= 0)
			mcycles--;
		else
			mcycles++;
	}

	for(i=0; i<15; i++)
	{
		if ((int16_t)aquot >= 0)
			mcycles++;

		aquot <<= 1;
	}

	return mcycles * 2;
}

int getDivs68kCycles(int32_t dividend, int16_t divisor)
{
	int v = getDivs68kCycles_2(dividend, divisor) - 4;
	return v;
}