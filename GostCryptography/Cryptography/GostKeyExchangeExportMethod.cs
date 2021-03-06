﻿namespace GostCryptography.Cryptography
{
	/// <summary>
	/// Алгоритм экспорта общего секретного ключа.
	/// </summary>
	public enum GostKeyExchangeExportMethod
	{
		/// <summary>
		/// Простой экспорт ключа по ГОСТ 28147-89.
		/// </summary>
		GostKeyExport,

		/// <summary>
		/// Защищённый экспорт ключа по алгоритму КриптоПро.
		/// </summary>
		CryptoProKeyExport
	}
}